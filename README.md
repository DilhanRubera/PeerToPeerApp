# Peer to Peer App 

## Overview

This project builds a simple peer-to-peer system where clients can post and execute jobs in Python. It consists of three main components:

1. **ASP.NET MVC Web Service** – Manages client registration and facilitates communication between clients.
2. **Desktop Application with .NET Remoting** – Connects to the Web Service, finds jobs, and executes them.
3. **ASP.NET Core Website** – Displays job completion stats and client information on a dynamic dashboard.

## Features

- **Web Service**: Allows clients to register and request other client information.
- **Desktop Application**: Fetches jobs, executes Python scripts, and reports results.
- **Website**: Dynamically updates job stats every minute.
