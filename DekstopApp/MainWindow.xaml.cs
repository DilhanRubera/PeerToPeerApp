using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using RestSharp;
using DLL;
using System.ServiceModel;
using Microsoft.Scripting.Hosting;
using IronPython.Hosting;
using Microsoft.Scripting.Runtime;
using Microsoft.Win32;
using System.IO;
using System.Threading;
using System.Security.Cryptography;


namespace DekstopApp
{

    public partial class MainWindow : Window
    {
        private IJobServer foob;

        private int clientId;
        private int jobId = 0;
        private int  noOfJobsCompleted = 0;
        private int userPort;
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            int port;
            string portText = PortInputBox.Text;

            if(!int.TryParse(portText, out port))
            {
                MessageBox.Show("Please enter an integer between 8000 and 82000");
                return;
            }
        

            List<Client> clients = await GetAllClients();
            if(clients != null)
            {
                bool isPortInUse = clients.Any(client => client.Port == port);
                if (!isPortInUse)
                {
                    DebugText.Text += "Entered port is not in use\n";

                    bool isAdded = await AddClientToClientList(port);

                    if (isAdded == true)
                    {
                        DebugText.Text += "Client added\n";
                        Thread serverThread = new Thread(()=>StartServer(port));
                        Thread jobStatusThread = new Thread(() => StartJobStatusChecker());
                        Thread networkThread = new Thread(() => StartNetwork());
                        serverThread.Start();
                        jobStatusThread.Start();
                        networkThread.Start();
                    }
                }
                else
                {
                    MessageBox.Show("Port is already in use by another client.");
                }
            }
            


        }

        private async Task<List<Client>> GetAllClients()
        {
            RestClient restClient = new RestClient("http://localhost:5066");
            RestRequest restRequest = new RestRequest("/api/clients", Method.Get);
            RestResponse restResponse = await restClient.ExecuteAsync(restRequest);
            List<Client> clientList = JsonConvert.DeserializeObject<List<Client>>(restResponse.Content);

            return clientList;
        }

        private async Task<bool> AddClientToClientList(int port)
        {
            bool added = false;
            RestClient restClient = new RestClient("http://localhost:5066");
            RestRequest restRequest = new RestRequest("/api/clients", Method.Post);
            Client client = new Client();
            restRequest.AddJsonBody(client);

            client.Port = port;
            userPort = port;


            RestResponse restResponse = await restClient.ExecuteAsync(restRequest);
            if (restResponse.IsSuccessful)
            {
                MessageBox.Show("Client registered successfully!");
                Client createdClient = JsonConvert.DeserializeObject<Client>(restResponse.Content);
                clientId = createdClient.ClientId;
                added = true;
            }
            else
            {
                MessageBox.Show("Failed to register client.");

            }

            return added;
        }

        private  void StartServer(int port)
        {

            string URL = $"net.tcp://localhost:{port}/PeerToPeerService";

            ServiceHost host;
            NetTcpBinding tcp = new NetTcpBinding();
            host = new ServiceHost(typeof(JobServerImpl));
            host.AddServiceEndpoint(typeof(IJobServer), tcp, URL);
            Application.Current.Dispatcher.Invoke(() =>
            {
                DebugText.Text += "Server started\n";
            });
            host.Open();
           

        }

   
        private async void StartNetwork()
        {
           
            Application.Current.Dispatcher.Invoke(() =>
            {
                DebugText.Text += "Network started\n";
            });
            while (true)
            {
                List<Client> clients = await GetAllClients();
               
                Application.Current.Dispatcher.Invoke(() =>
                {
                 //   DebugText.Text += "Got clients from database\n";
                });
                foreach (var client in clients)
                {
                    try
                    {
                        foob = EstablishClientConnection(client.Port);
                        if (!(client.Port == userPort))
                        {

                            Job job = foob.GetJob();

                            if (job != null)
                            {
                                string result = ExecutePythonJob(job.PythonScript, job.ScriptHash);
                                foob.SubmitJobResult(result, job.JobId);
                                IncrementClientsNoOfTasksCompleted();
                            }

                        }
                    } catch(Exception e)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DebugText.Text += "Error Occured" + e.Message;
                        });
                      
                    }

                }
                Thread.Sleep(1000);
            }

        }

        private void StartJobStatusChecker()
        {
            while (true)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                   JobStatusListBox.Items.Clear();
                });

                var jobs = JobManager.JobQueue;

                if (jobs != null)
                {
                    foreach (var job in jobs)
                    {
                        if (!job.IsCompleted && !job.InProgress)
                        {
                            UpdateJobStatus($"Job ID: {job.JobId} - Not Assigned");
                        }
                        else if (!job.IsCompleted && job.InProgress)
                        {
                            UpdateJobStatus($"Job ID: {job.JobId} - In Progress");
                        }
                        else if (job.IsCompleted && !job.InProgress)
                        {
                            UpdateJobStatus($"Job ID: {job.JobId} - Completed - Result : {job.JobResult}");
                        }
                    }
                }

                Thread.Sleep (1000);
            }
        }
        private void UpdateJobStatus(string status)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                JobStatusListBox.Items.Add(status);
            });
        }

        private IJobServer EstablishClientConnection(int clientPort)
        {
            ChannelFactory<IJobServer> foobFactory;
            NetTcpBinding tcp = new NetTcpBinding();
            string URL = $"net.tcp://localhost:{clientPort}/PeerToPeerService";
            foobFactory = new ChannelFactory<IJobServer>(tcp, URL);
            IJobServer returnFoob =  foobFactory.CreateChannel();
            return returnFoob;

        } 

        private  string ExecutePythonJob(string script, string expectedHash)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                DebugText.Text += "Executing Python job\n";
            });

            if (!VerifySHA256Hash(script, expectedHash))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DebugText.Text += "Hash mismatch! Script may be corrupted.\n";
                });
                return "Hash mismatch!";
            }
            string decodedScript = Base64Decode(script);

            ScriptEngine scriptEngine = Python.CreateEngine();
            ScriptScope scriptScope = scriptEngine.CreateScope();
            scriptEngine.Execute(decodedScript, scriptScope);

            dynamic result = scriptScope.GetVariable("result");
            Application.Current.Dispatcher.Invoke(() =>
            {
                DebugText.Text += "Result: "+result+"\n";
            });
            return result.ToString();

        }

        private void UploadFileBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Python files (*.py)|*.py|All files (*.*)|*.*",
                Title = "Select a Python script file"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string fileContent = File.ReadAllText(openFileDialog.FileName);        
                    ScriptInputBox.Text = fileContent;
                    AddJob(fileContent);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading file: {ex.Message}");
                }
            }

        }

        private void AddJob(string script)
        {
            string encodedScript = Base64Encode(script);
            string scriptHash = ComputeSHA256Hash(encodedScript);

            Job job = new Job();
            job.PythonScript = encodedScript;
            job.JobId = jobId;
            jobId += 1;
            job.ScriptHash = scriptHash;
           JobManager.JobQueue.Add(job);
            Application.Current.Dispatcher.Invoke(() =>
            {
                DebugText.Text += "Job added\n";
            });
        }

        private void SendScriptBtn_Click(object sender, RoutedEventArgs e)
        {
            string script = ScriptInputBox.Text;
            AddJob(script);
        }

        private async void IncrementClientsNoOfTasksCompleted()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                DebugText.Text += "Increment no of tasks completed\n";
            });

            noOfJobsCompleted++;
            Application.Current.Dispatcher.Invoke(() =>
            {
                TotalJobsBox.Clear();
                TotalJobsBox.Text += noOfJobsCompleted;
            });
            RestClient restClient = new RestClient("http://localhost:5066");
            RestRequest restRequest = new RestRequest($"/api/clients/incrementNoOfTasksCompleted/{clientId}", Method.Put);

            RestResponse restResponse = await restClient.ExecuteAsync(restRequest);

            if (restResponse.IsSuccessful)
            {
                Console.WriteLine("Client's completed tasks updated");
            }
            else
            {
                Console.WriteLine("Error updating client's completed task");
            }

        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ExitApplication();
        }

        private async void ExitApplication()
        {
            RestClient restClient = new RestClient("http://localhost:5066");
            RestRequest restRequest = new RestRequest($"/api/clients/{clientId}", Method.Delete);
            RestResponse restResponse = await restClient.ExecuteAsync(restRequest);
            if (restResponse.IsSuccessful)
            {
                Application.Current.Shutdown();

            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DebugText.Text += "Unable to exit application. Please try again\n";
                });
            }
        }
        public string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public string ComputeSHA256Hash(string data)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(data));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public bool VerifySHA256Hash(string data, string hash)
        {
            string hashOfInput = ComputeSHA256Hash(data);
            return StringComparer.OrdinalIgnoreCase.Compare(hashOfInput, hash) == 0;
        }
    }
}
