﻿namespace Pici.Storage.Database
{
    using Pici.Storage.Database.Database_Exceptions;
    using Pici.Storage.Database.Session_Details.Interfaces;
    using Pici.Storage.Managers.Database;
    using MySql.Data.MySqlClient;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Text;
    using System.Collections;
    using System.Data.SqlClient;

    public class DatabaseManager
    {
        private int beginClientAmount;
        private string connectionString;
        private List<MySqlClient> databaseClients;
        private bool isConnected = false;
        private uint maxPoolSize;
        private uint minPoolSize;
        private DatabaseServer server;
        private Queue connections;
        internal DatabaseType type { get; set; }

        public static bool dbEnabled = true;

        public DatabaseManager(uint maxPoolSize, uint minPoolSize, int clientAmount, DatabaseType dbType)
        {
            if (maxPoolSize < clientAmount)
                throw new DatabaseException("The poolsize can not be larger than the client amount!");

            this.type = dbType;
            this.beginClientAmount = clientAmount;
            this.maxPoolSize = maxPoolSize;
            this.minPoolSize = minPoolSize;
            this.connections = new Queue();
        }

        private void addConnection(int id)
        {
            MySqlClient item = new MySqlClient(this, id);
            item.connect();
            this.databaseClients.Add(item);
        }

        private void createNewConnectionString()
        {
            if (this.type == DatabaseType.MySQL)
            {
                MySqlConnectionStringBuilder connectionString = new MySqlConnectionStringBuilder
                {
                    Server = this.server.getHost(),
                    Port = this.server.getPort(),
                    UserID = this.server.getUsername(),
                    Password = this.server.getPassword(),
                    Database = this.server.getDatabaseName(),
                    MinimumPoolSize = this.minPoolSize,
                    MaximumPoolSize = this.maxPoolSize,
                    AllowZeroDateTime = true,
                    ConvertZeroDateTime = true,
                    DefaultCommandTimeout = 300,
                    ConnectionTimeout = 10
                };

                this.setConnectionString(connectionString.ToString());
            }
            else
            {
                SqlConnectionStringBuilder connectionString = new SqlConnectionStringBuilder
                {
                    DataSource = this.server.getHost(),
                    //Port = this.server.getPort(),
                    UserID = this.server.getUsername(),
                    Password = this.server.getPassword(),
                    InitialCatalog = this.server.getDatabaseName(),
                    MinPoolSize = (int)this.maxPoolSize / 2,
                    MaxPoolSize = (int)this.maxPoolSize,
                    //AllowZeroDateTime = true,
                    //ConvertZeroDateTime = true,
                    //DefaultCommandTimeout = 300,
                    ConnectTimeout = 10,
                    Pooling = true
                };

                this.setConnectionString(connectionString.ToString());
            }
        }

        public void destroy()
        {
            lock (this)
            {
                this.isConnected = false;
                if (this.databaseClients != null)
                {
                    foreach (MySqlClient client in this.databaseClients)
                    {
                        if (!client.isAvailable())
                        {
                            client.Dispose();
                        }
                        client.disconnect();
                    }
                    this.databaseClients.Clear();
                }
            }
        }

        private void disconnectUnusedClients()
        {
            lock (this)
            {
                foreach (MySqlClient client in this.databaseClients)
                {
                    if (client.isAvailable())
                    {
                        client.disconnect();
                    }
                }
            }
        }

        internal string getConnectionString()
        {
            return this.connectionString;
        }

        public IQueryAdapter getQueryreactor()
        {
            IDatabaseClient dbClient = null;
            lock (connections.SyncRoot)
            {
                if (connections.Count > 0)
                {
                    dbClient = (IDatabaseClient)connections.Dequeue();
                }
            }

            if (dbClient != null)
            {
                dbClient.connect();
                dbClient.prepare();
                return dbClient.getQueryReactor();
            }
            else
            {
                if (type == DatabaseType.MySQL)
                {
                    IDatabaseClient connection = new MySqlClient(this, 0);
                    connection.connect();
                    connection.prepare();
                    return connection.getQueryReactor();
                }
                else
                {
                    IDatabaseClient connection = new MsSQLClient(this, 0);
                    connection.connect();
                    connection.prepare();
                    return connection.getQueryReactor();
                }
            }
        }

        internal void FreeConnection(IDatabaseClient dbClient)
        {
            lock (connections.SyncRoot)
            {
                connections.Enqueue(dbClient);
            }
        }

        public void init()
        {
            try
            {
                this.createNewConnectionString();
                this.databaseClients = new List<MySqlClient>((int) this.maxPoolSize);
            }
            catch (MySqlException exception)
            {
                this.isConnected = false;
                throw new Exception("Could not connect the clients to the database: " + exception.Message);
            }
            this.isConnected = true;
        }

        public bool isConnectedToDatabase()
        {
            return this.isConnected;
        }

        private void setConnectionString(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public bool setServerDetails(string host, uint port, string username, string password, string databaseName)
        {
            try
            {
                this.server = new DatabaseServer(host, port, username, password, databaseName);
                return true;
            }
            catch (DatabaseException)
            {
                this.isConnected = false;
                return false;
            }
        }
    }
}

