using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace DLS
{
    /**
     * This class is the implementation of the ILogService class. 
     * Any client which needs to communicate to the server can use this class. 
     * 
     * @author : Kumar Chandan
     */
    class LogClient : ILogService
    {
        //constants 
        private const string NEW_TKT_CMD = "0", LOG_MSG_CMD = "1",
            RELEASE_TKT_CMD = "2", RETRIEVE_MSGS_CMD = "3";

        StreamReader streamReader;
        StreamWriter streamWriter;
       
       /**
        * Open a connection with a log server.
        * 
        * @param host name of the host to connect to
        * @param port the port the server is listening on
        * 
        */
        public void open(String host, int port) {
            try
            {
                TcpClient conn = new TcpClient(host, port);
                streamReader = new StreamReader(conn.GetStream());
                streamWriter = new StreamWriter(conn.GetStream());
            }

            catch (Exception e) { 
                Console.Error.WriteLine("Error in setting up TCP connection" + e.GetType()); 
            }
        }

        /**
         * Close the connection with the server
         */
        public void close() {
            streamReader.Close();
            streamWriter.Close();
        }

        /**
        * Obtain a new ticket.
        *
        * @return the ticket returned by the server
        *
        */
        public String newTicket() {
            streamWriter.WriteLine(NEW_TKT_CMD);
            streamWriter.Flush();
            return streamReader.ReadLine();
        }

        /**
        * Add an entry to the log identified by the specified ticket
        *
        * @param ticket the ticket of the log to be written to
        * @param message the message to be written to the log
        */
        public void addEntry(String ticket, String message)
        {
            streamWriter.WriteLine(LOG_MSG_CMD + ticket + ":" + message);
            streamWriter.Flush();
        }

        /**
        * Get a list of all the entries that have been written to the log
        * identified by the given ticket.
        *
        * @param ticket the ticket that identifies the log
        *
        * @return a list containing all of the entries written to the log
        *
        */
        public List<String> getEntries(String ticket) {
            streamWriter.WriteLine(RETRIEVE_MSGS_CMD + ticket);
            streamWriter.Flush();
            
            int messagesCount = Convert.ToInt16(streamReader.ReadLine());
            /*if(messagesCount == 0)
                return null;*/

            List<String> messagesList = new List<String>();
            for(int i=0; i < messagesCount; i++){
                messagesList.Add(streamReader.ReadLine());       
            }
            
            return messagesList;
        }

        /**
        * Release the spcified ticket.  The entries associated with the
        * ticket will no longer be available
        *
        * @param ticket the ticket to be released
        */
        public void releaseTicket(String ticket)
        {
            streamWriter.WriteLine(RELEASE_TKT_CMD + ticket);
            streamWriter.Flush();
        }
    }
}
