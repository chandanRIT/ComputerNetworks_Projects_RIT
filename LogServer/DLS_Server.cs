using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace DLS
{
    /**
     * Class for the 'Distributed Logging Server'
     */
    class DLS_Server
    {
        //constants
        private const string NEW_TKT_CMD = "0", LOG_MSG_CMD = "1",
            RELEASE_TKT_CMD = "2", RETRIEVE_MSGS_CMD = "3";
        private const short PORT_NUMBER = 6007;

        private TcpListener listener;
        private static StreamWriter logFile, debugFile;
        private Dictionary<Guid, List<String>> listOfLiveTickets = new Dictionary<Guid, List<String>>();

        /**
         * Constructor, which starts the server, log files
         */
        public DLS_Server() {
            logFile = new StreamWriter("LogServer.log");
            //writeToFile(true, "LogFile started");

            debugFile = new StreamWriter("LogServer.debug");
            writeToFile(false, "Server Started on port" + PORT_NUMBER);

            //The code below is commented since it doesnot run for when doing: telnet <localhost> <portnumber>
            /*IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            IPAddress ipV4 = null;
            foreach (IPAddress eachIP in localIPs)
            {
                if (eachIP.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipV4 = eachIP;
                    break;
                }
            }*/
            listener = new TcpListener(IPAddress.Any, PORT_NUMBER);
            listener.Start();
        }

        /**
         * 'Main' method, the entry point for any program in C#
         * 
         * @param args : command-line arguments
         */
        static void Main(string[] args)
        {
            DLS_Server server;

            try
            {
                server = new DLS_Server();
            } catch (Exception e){
                writeToFile(false, "Error starting server : --> " + e.GetType());
                return;
            }

            //keep accepting any new client
            while (true)
            {
                try
                {
                    //do client handling in an synchronous function
                    server.listener.BeginAcceptTcpClient(server.DoAcceptTcpClientCallback, server.listener);
                } catch(Exception e) { //handling exception when starting server
                    writeToFile(false, "Error in accepting client : --> " + e.GetType());
                }
                // connectionWaitHandle.WaitOne(); //Wait until a client has begun handling an event
            }
            //logFile.Close();
            //debugFile.Close();
            //server.listener.Stop();
        }

        /**
         * checks if a ticket is live or has been released
         * 
         * @param ticketAsGUID
         */
        bool checkIfTicketIsLive(Guid ticketAsGUID){
            if (!listOfLiveTickets.ContainsKey(ticketAsGUID)) {
                writeToFile(false, "Error : Ticket Not Found");
                return false;
            }
            return true;
        }

        /**
         * Writes the text to a file (a log file or debug file). Mainly to avoid doing flush every time.
         * 
         * @param logOrDebugFile : A flag which indicates which file to write to. True, for LogServer.log file , False : for LogServer.debug
         * @param text : text to write into the file
         */
        static void writeToFile(bool logOrDebugFile, String text)
        {
            if (logOrDebugFile) // true for LogFile
            {
                logFile.WriteLine(text);
                logFile.Flush();
            }
            else {
                debugFile.WriteLine(text);
                debugFile.Flush();
            }

        }

        /**
         * Call back function which gets called on an incoming client connection. 
         * 
         * @param ar : This IAsyncResult contains the information regarding aynchronous operation.
         */
        public void DoAcceptTcpClientCallback(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener)ar.AsyncState;
            TcpClient clientSocket = listener.EndAcceptTcpClient(ar);
            NetworkStream networkStream = clientSocket.GetStream();
            StreamReader streamReader = new StreamReader(networkStream);
            StreamWriter streamWriter = new StreamWriter(networkStream);

            writeToFile(false, "Client Accepted");

            //keep listening from the client
            while (true)
            {
                try
                {
                    String lineRead = streamReader.ReadLine();

                    if (lineRead == null)
                        continue;

                    if (lineRead.Trim().Equals(NEW_TKT_CMD)) // handling '0' : New Ticket Command
                    {
                        Guid newTicket = Guid.NewGuid();
                        listOfLiveTickets.Add(newTicket, new List<String>());
                        streamWriter.WriteLine(newTicket);
                        streamWriter.Flush();
                        writeToFile(false, newTicket + " generated");
                    }

                    else if (lineRead.StartsWith(RELEASE_TKT_CMD)) // handling '2' : Release Ticket Command
                    {
                        String ticketAsString;
                        //if substring fails for if the string is just of length '1'
                        try
                        {
                            ticketAsString = lineRead.Substring(1).Trim();
                        }
                        catch (ArgumentOutOfRangeException aore)
                        {
                            writeToFile(false, "Error parsing arguments for command '2': --> " + aore.GetType());
                            continue;
                        }
                   
                        try
                        {
                            Guid ticketAsGuid = Guid.Parse(ticketAsString);
                            if (!checkIfTicketIsLive(ticketAsGuid))
                                continue;

                            listOfLiveTickets.Remove(ticketAsGuid);
                            writeToFile(false, "Key Released for ticket : " + ticketAsString);
                        }
                        catch (FormatException fe)
                        {
                            writeToFile(false, "Error parsing arguments for command '2': --> " + fe.GetType());
                            continue;
                        }

                    }

                    else if ((lineRead.StartsWith(LOG_MSG_CMD))) // handling '1' : Log Message Command
                    {
                        int indexOfColon = lineRead.IndexOf(":");
                        if (indexOfColon < 0)
                        {
                            writeToFile(false, "Error in the arguments for command '1'");
                            continue;
                        }

                        String ticketAsString;
                        try
                        {
                            ticketAsString = lineRead.Substring(1, indexOfColon - 1);
                        }
                        catch (ArgumentOutOfRangeException aore)
                        {
                            writeToFile(false, "Error in the arguments for command '1' : --> " + aore.GetType());
                            continue;
                        }
                       
                        Guid ticketAsGUID;
                        try //handling ticket parsing
                        {
                            ticketAsGUID = Guid.Parse(ticketAsString);
                        }
                        catch (FormatException fe)
                        {
                            writeToFile(false, "Error in the arguments for command '1' : --> " + fe.GetType());
                            continue;
                        }

                        if (!checkIfTicketIsLive(ticketAsGUID))
                            continue;

                        //check if any message exists after ':'
                        String message;
                        try
                        {
                            message = lineRead.Substring(indexOfColon + 1);
                        }
                        catch (ArgumentOutOfRangeException aore)
                        {
                            writeToFile(false, "Error: no message found" + aore);
                            continue;
                        }

                        listOfLiveTickets[ticketAsGUID].Add(message);
                        writeToFile(false, "Client logged message succesfully");
                        writeToFile(true, ticketAsString + " - " + message);
                    }

                    else if ((lineRead.StartsWith(RETRIEVE_MSGS_CMD))) // handling '3' : Retrieve Messages Command
                    {
                        //if this command has no extra arguments
                        if (lineRead.Length == 1)
                        {
                            writeToFile(false, "Error parsing arguments for command '3'");
                            continue;
                        }

                        String ticketAsString = lineRead.Substring(1).Trim();

                        //writeToFile(false, ticketAsString);
                        Guid ticketAsGUID;
                        //checking ticket parsing
                        try
                        {
                            ticketAsGUID = Guid.Parse(ticketAsString);
                        }
                        catch (FormatException fe)
                        {
                            writeToFile(false, "Error parsing arguments for command '3' : --> " + fe.GetType());
                            continue;
                        }

                        if (!checkIfTicketIsLive(ticketAsGUID))
                        {
                            streamWriter.WriteLine("0");
                            streamWriter.Flush();
                            continue;
                        }
                        //write back the count and the list of messages
                        streamWriter.WriteLine(listOfLiveTickets[ticketAsGUID].Count);
                        foreach (String message in listOfLiveTickets[ticketAsGUID])
                        {
                            streamWriter.WriteLine(message);
                        }
                        streamWriter.Flush();
                        writeToFile(false, "Client retrieved messages succesfully");
                    }

                    else //All unrecognized commands get handled here
                    {
                        writeToFile(false, "Unrecognized Command passed");
                        continue;
                    }
                } catch (Exception e){ // especially, for if the client closes the connection suddenly
                    writeToFile(false, "Client Closed Connection : --> " + e.GetType());
                    break;
                }
            }
            
            //close the streams linked to the client
            streamReader.Close();
            streamWriter.Close();
        
        }

    }
}





