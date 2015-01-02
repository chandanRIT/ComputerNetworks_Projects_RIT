/// Interface for Logging client
/// Jeremy S. Brown (jsb@cs.rit.edu)
/// Ported from Java logging client by Paul Tymann

using System;
using System.Collections.Generic;
using System.Text;

namespace DLS
{
    interface ILogService
    {

        /**
         * Open a connection with a log server.
         * 
         * @param host name of the host to connect to
         * @param port the port the server is listening on
         * 
         */
        void open(String host, int port);

        /**
         * Close the connection with the server
         */
        void close();

        /**
         * Obtain a new ticket.
         *
         * @return the ticket returned by the server
         *
         */
        String newTicket();

        /**
         * Add an entry to the log identified by the specified ticket
         *
         * @param ticket the ticket of the log to be written to
         * @param message the message to be written to the log
         */
        void addEntry(String ticket, String message);

        /**
         * Get a list of all the entries that have been written to the log
         * identified by the given ticket.
         *
         * @param ticket the ticket that identifies the log
         *
         * @return a list containing all of the entries written to the log
         *
         */
        List<String> getEntries(String ticket);

        /**
         * Release the spcified ticket.  The entries associated with the
         * ticket will no longer be available
         *
         * @param ticket the ticket to be released
         */
        void releaseTicket(String ticket);

    }
}
