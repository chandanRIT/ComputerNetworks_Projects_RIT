/*
 * FTP Sample class
 * Jeremy Brown (jsb@cs.rit.edu)
 */
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace FTP
{
    /// <summary>
    /// Sample FTP class for DataComm I
    /// Authors Jeremy S. Brown, Kumar Chandan
    /// </summary>
    /**
     * This class represents an RFC-959 FTP client
     */
    class Ftp
    {
        static int FTP_PORT = 21;

        //TCPClients for control-connection and passive mode
        TcpClient clientForCmdConn, passvModeClient;
        
        //Stream Readers/Writers on client's control sockect
        StreamReader streamReaderCmdConn;
        StreamWriter streamWriterCmdConn;

        string sfileName;

        //instance level variables for toggling certain parameters
        bool isClosedConn = false, isPassive = false, 
            inDefaultMode = true, inDebugMode = false;

        //client-side TCPListerner for the active mode
        TcpListener activeModeListener;

        // The prompt
        public const string PROMPT = "FTP> ";

        // Information to parse commands
        public static readonly string[] COMMANDS = { 
                          "ascii",
					      "binary",
					      "cd",
					      "cdup",
					      "debug",
					      "dir",
					      "get",
					      "help",
					      "passive",
                          "put",
                          "pwd",
                          "quit",
                          "user",
                          "mkdir",
                          "nlist"};

        public const int ASCII = 0;
        public const int BINARY = 1;
        public const int CD = 2;
        public const int CDUP = 3;
        public const int DEBUG = 4;
        public const int DIR = 5;
        public const int GET = 6;
        public const int HELP = 7;
        public const int PASSIVE = 8;
        public const int PUT = 9;
        public const int PWD = 10;
        public const int QUIT = 11;
        public const int USER = 12;
        public const int MKDIR = 13;
        public const int NLIST = 14;

        // Help message

        public static readonly String[] HELP_MESSAGE = {
	"ascii      --> Set ASCII transfer type",
	"binary     --> Set binary transfer type",
	"cd <path>  --> Change the remote working directory",
	"cdup       --> Change the remote working directory to the",
        "               parent directory (i.e., cd ..)",
	"debug      --> Toggle debug mode",
	"dir        --> List the contents of the remote directory",
	"get path   --> Get a remote file",
	"help       --> Displays this text",
	"passive    --> Toggle passive/active mode",
    "put path   --> Transfer the specified file to the server",
	"pwd        --> Print the working directory on the server",
    "quit       --> Close the connection to the server and terminate",
	"user login --> Specify the user name (will prompt for password",
    "mkdir       --> create a directory in the remote server",
    "nlist       --> Returns a list of filenames in the given directory",};
               
        /**
         * Sets up command/control connection on which the command-related data is sent and received
         * @param host : host-name
         */
        void setupCmdConn(string host){
            try
            {
                clientForCmdConn = new TcpClient(host, FTP_PORT);
            }
            catch {
                Console.Error.WriteLine(host + ": Name or service not known");
                Environment.Exit(1);
            }
            streamReaderCmdConn = new StreamReader(clientForCmdConn.GetStream());
            streamWriterCmdConn = new StreamWriter(clientForCmdConn.GetStream());
            streamWriterCmdConn.AutoFlush = true;
            printReply(streamReaderCmdConn);

            prompForLogin();
        }

        /**
         * present the login prompt
         */
        void prompForLogin(){
            Boolean askForPwd = false;
            while (true)
            {
                Console.Write("Name:");
                string user = Console.ReadLine();
                streamWriterCmdConn.WriteLine("USER " + user);
                
                string response = printReply(streamReaderCmdConn);
                if (response == null) {
                    return;
                }

                if (response.StartsWith("500 ")) //needs some user name 
                    continue;
                else if (response.StartsWith("331 ")) //user-name accepted, asking for password
                {
                    askForPwd = true;
                }

                break;
             }

            if (askForPwd) {
                Console.Write("Password:");
                string pass = Console.ReadLine();
                streamWriterCmdConn.WriteLine("PASS " + pass);
                printReply(streamReaderCmdConn);
            }
        }

        /**
         * print the replies on the passed StreamReader 
         * @param streamReader : the StreamReader on which to read the replies/data on.
         * @returns the last line read
         */
        string printReply(StreamReader streamReader, bool toPrint = true){
            string lineRead;
            while(true){
                lineRead = streamReader.ReadLine();
                if (lineRead == null)
                {
                    if (streamReader == streamReaderCmdConn)
                    {
                        isClosedConn = true;
                        Console.WriteLine("remote server has closed connection");
                    }
                    return null;
                }
                if(toPrint)
                    Console.WriteLine(lineRead);
                if (lineRead.Length >= 4)
                {
                    string substring = lineRead.Substring(0, 4);
                    if (substring.IndexOf(" ") == 3) //if it has a space at its index = 3 
                    {
                        try
                        {
                            if (Convert.ToInt16(substring.Substring(0, 3)) == 421) //and is a 3-digit number
                            {
                                isClosedConn = true;
                                return null;
                            }
                            break;
                        }
                        catch (FormatException) { }
                    }
                }
            }
            return lineRead;
        }

        /**
         * sets up the Passive Data connection
         * @param host name
         */
        bool setupPasvDataConn(string host)
        {
            streamWriterCmdConn.WriteLine("PASV");
            string response = printReply(streamReaderCmdConn);
            if(!response.StartsWith("227 "))
                return false;
            string[] tokens = response.Split(',');

            try
            {
                passvModeClient = new TcpClient(host, Convert.ToInt16(tokens[4]) * 256 +
                    Convert.ToInt16(tokens[5].Substring(0, tokens[5].Length - 2)));
            } catch(SocketException){
                Console.WriteLine("Error establishing passive data connection");
                return false;
            }
            //StreamReader strmRdrPasvDataConn = new StreamReader(passvModeClient.GetStream());
            //StreamWriter strmWrtrPasvDataConn = new StreamWriter(clientForCmdConn.GetStream());
            //strmWrtrPasvDataConn.AutoFlush = true;
            return true;
        }

        /**
         * handles the 'dir' command
         * @param host: host-name
         */
        void handleDirCmd(string host){
            if (isPassive)
            {
                if(!setupPasvDataConn(host))
                    return;
                sendDIRcmd();
                printReply(new StreamReader(passvModeClient.GetStream()));
                passvModeClient.Close();
            }
            else {
                if (!setupActvDataConn())
                    return;
                sendDIRcmd();
                activeModeListener.BeginAcceptTcpClient(acceptTcpClientCallback, activeModeListener);
            }
            printReply(streamReaderCmdConn);
        }

        /**
         * sets up the active data connection
         */
        bool setupActvDataConn() {
            IPAddress ipAddr = null;
            foreach (IPAddress ip in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddr = ip;
                }
            }

            try
            {
                activeModeListener = new TcpListener(ipAddr, 0); // '0' means any random unused non-reserved port
                activeModeListener.Start();
            }
            catch (Exception) {
                Console.WriteLine("Error setting up command Connection");
                activeModeListener = null;
                return false;
            }

            IPEndPoint ipEndPoint = (IPEndPoint) activeModeListener.LocalEndpoint;
            streamWriterCmdConn.WriteLine("PORT " + ipAddr.ToString().Replace('.',',') + "," + ipEndPoint.Port/256 + "," + ipEndPoint.Port%256);
            string response = printReply(streamReaderCmdConn); // reply from FTP-server is "200 port cmd successful"
            if (!response.StartsWith("200 "))
            {
                activeModeListener = null;
                return false;
            }
            return true;
        }

        /**
         * sends the LIST command to the FTP server
         */
        void sendDIRcmd() {
            streamWriterCmdConn.WriteLine("LIST");
            printReply(streamReaderCmdConn);
        }

        /**
         * callback function for asynchronous client accepting
         */
        public void acceptTcpClientCallback(IAsyncResult ar) {
            TcpListener listener = (TcpListener)ar.AsyncState;
            TcpClient clientSocket = listener.EndAcceptTcpClient(ar);
            if (sfileName != null)
            {
                doStreamTransfer(clientSocket.GetStream(), File.Open(sfileName, FileMode.Create));
                sfileName = null;
            }
            else
                printReply(new StreamReader(clientSocket.GetStream()));
            clientSocket.Close();
            listener.Stop();
        }

        /**
         * handles the GET command
         * @param host : hots-name
         * @param fileName : fileName to get from the FTP server
         */
        void handleGetCmd(string host, string fileName) {
            if (isPassive) {
                if(!setupPasvDataConn(host))
                    return;
                if(!sendGETCmd(fileName)) //remote-file not found
                    return;
                doStreamTransfer(passvModeClient.GetStream(), File.Open(fileName, FileMode.Create));
                passvModeClient.Close();
            }
            else
            {
                if (!setupActvDataConn())
                    return;
                if (!sendGETCmd(fileName)) //remote-file not found
                    return;
                sfileName = fileName;
                activeModeListener.BeginAcceptTcpClient(acceptTcpClientCallback, activeModeListener);
            }
            printReply(streamReaderCmdConn);
        }

        /**
         * sends the RETR command to the server
         * @param fileName: fileName to get from the FTP server
         */
        bool sendGETCmd(string fileName){
            streamWriterCmdConn.WriteLine("RETR " + fileName);
            return !printReply(streamReaderCmdConn).StartsWith("550 "); //return true when remote-file is found
        }

        /**
         * transfers from source stream to the destination stream
         * @param src: src stream
         * @param dst : dst stream
         */
        void doStreamTransfer(Stream src, Stream dst){
            int readCount;
            var buffer = new byte[8192];
            while ((readCount = src.Read(buffer, 0, buffer.Length)) != 0)
                dst.Write(buffer, 0, readCount);
            src.Close();
            dst.Close();
        }

        /**
         * handles the cd command
         * @param argv secondary inputs for the commands
         */
        void handleCDCmd(string[] argv){
            string input = "";
            if(argv.Length >= 2)
                input = argv[1];
           
            streamWriterCmdConn.WriteLine("CWD " + input);
            printReply(streamReaderCmdConn);
            if (input.Length == 0)
                Console.WriteLine("usage: cd <remote-directory>");
        }

        /**
         * handles the mkdir command
         * @param argv secondary inputs for the commands
         */
        void handleMKDCmd(string[] argv){
            string input = "";
            if (argv.Length >= 2)
                input = argv[1];

            streamWriterCmdConn.WriteLine("MKD " + input);
            printReply(streamReaderCmdConn);
            if (input.Length == 0)
                Console.WriteLine("usage: mkdir <directory-name>");
        }

        /**
         * handles the nlist command
         * @param argv secondary inputs for the commands
         */
        void handleNLSTCmd(string host)
        {
            if (isPassive)
            {
                if (!setupPasvDataConn(host))
                    return;
                streamWriterCmdConn.WriteLine("NLST");
                printReply(streamReaderCmdConn);
                printReply(new StreamReader(passvModeClient.GetStream()));
                passvModeClient.Close();
            }
            else
            {
                if (!setupActvDataConn())
                    return;
                streamWriterCmdConn.WriteLine("NLST");
                printReply(streamReaderCmdConn);
                activeModeListener.BeginAcceptTcpClient(acceptTcpClientCallback, activeModeListener);
            }
            printReply(streamReaderCmdConn);
        }

        /**
         * the main method
         * @param args : cmd-line arguments
         */
        static void Main(string[] args)
        {
            // Handle the command line stuff
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: [mono] Ftp server");
                Environment.Exit(1);
            }

            //Scanner in = new Scanner( System.in );
            bool eof = false;
            String input = null;

            Ftp ftp = new Ftp();
            ftp.setupCmdConn(args[0]);

            // Command line is done - accept commands
            do
            {
                try
                {
                    Console.Write(PROMPT);
                    input = Console.ReadLine();
                }
                catch (Exception)
                {
                    eof = true;
                }

                // Keep going if we have not hit end of file
                if (!eof && input.Length > 0)
                {
                    int cmd = -1;
                    string[] argv = Regex.Split(input, "\\s+");

                    // What command was entered?
                    for (int i = 0; i < COMMANDS.Length && cmd == -1; i++)
                    {
                        if (COMMANDS[i].Equals(argv[0], StringComparison.CurrentCultureIgnoreCase))
                        {
                            cmd = i;
                        }
                    }

                    if (cmd != -1 && cmd != QUIT) {
                        if (ftp.isClosedConn) { Console.WriteLine("Not Connected."); continue; }
                    }

                    // Execute the command
                    switch (cmd)
                    {
                        case ASCII:
                            if(ftp.inDebugMode)
                                Console.WriteLine("---> Type A");
                            ftp.streamWriterCmdConn.WriteLine("TYPE A");
                            ftp.printReply(ftp.streamReaderCmdConn);
                            ftp.inDefaultMode = false;
                            break;

                        case BINARY:
                            if (ftp.inDebugMode)
                                Console.WriteLine("---> Type I");
                            ftp.streamWriterCmdConn.WriteLine("TYPE I");
                            ftp.printReply(ftp.streamReaderCmdConn);
                            ftp.inDefaultMode = false;
                            break;

                        case CD:
                            if (ftp.inDebugMode)
                                Console.WriteLine("---> CD command called");
                            ftp.handleCDCmd(argv);
                            break;

                        case CDUP:
                            if (ftp.inDebugMode)
                                Console.WriteLine("---> cdup command called");
                            ftp.streamWriterCmdConn.WriteLine("CDUP");
                            ftp.printReply(ftp.streamReaderCmdConn);
                            break;

                        case DEBUG:
                            ftp.inDebugMode = !ftp.inDebugMode;
                            if(ftp.inDebugMode)
                                Console.WriteLine("Debugging on (debug=1).");
                            else
                                Console.WriteLine("Debugging off (debug=0).");
                            break;

                        case DIR:
                            if (ftp.inDebugMode)
                                Console.WriteLine("---> dir command called");
                            ftp.handleDirCmd(args[0]);
                            break;

                        case NLIST:
                            if (ftp.inDebugMode)
                                Console.WriteLine("---> nlist command called");
                            ftp.handleNLSTCmd(args[0]);
                            break;

                        case GET:
                            if (ftp.inDebugMode)
                                Console.WriteLine("---> get command called");
                            string inputF = "";
                            if (argv.Length >= 2)
                                inputF = argv[1];
                            if (inputF.Length == 0)
                            {
                                Console.WriteLine("500 Invalid number of argumnets.");
                                Console.WriteLine("usage: cd <remote-file>");
                                break;
                            }
                            if (ftp.inDefaultMode)
                            {
                                ftp.streamWriterCmdConn.WriteLine("TYPE I");
                                ftp.printReply(ftp.streamReaderCmdConn, false);
                            }

                            ftp.handleGetCmd(args[0], inputF);

                            if (ftp.inDefaultMode)
                            {
                                ftp.streamWriterCmdConn.WriteLine("TYPE A");
                                ftp.printReply(ftp.streamReaderCmdConn, false);
                            }
                            break;

                        case HELP:
                            if (ftp.inDebugMode)
                                Console.WriteLine("---> help command called");
                            for (int i = 0; i < HELP_MESSAGE.Length; i++)
                            {
                                Console.WriteLine(HELP_MESSAGE[i]);
                            }
                            break;

                        case PASSIVE:
                            if (ftp.inDebugMode)
                                Console.WriteLine("---> passive command called");
                            ftp.isPassive = !ftp.isPassive; //toggle passive mode
                            Console.WriteLine("Passive mode " + (ftp.isPassive ? "on" : "off") + ".");
                            break;

                        case PUT:
                            if (ftp.inDebugMode)
                                Console.WriteLine("---> put command called");
                            break;

                        case PWD:
                            if (ftp.inDebugMode)
                                Console.WriteLine("---> pwd command called");
                            ftp.streamWriterCmdConn.WriteLine("PWD");
                            ftp.printReply(ftp.streamReaderCmdConn);
                            break;

                        case QUIT:
                            if (ftp.inDebugMode)
                                Console.WriteLine("---> quit command called");
                            eof = true;
                            if (!ftp.isClosedConn)
                            {
                                ftp.streamWriterCmdConn.WriteLine("QUIT");
                                ftp.printReply(ftp.streamReaderCmdConn);
                            }
                            ftp.clientForCmdConn.Close();
                            break;

                        case USER:
                            if (ftp.inDebugMode)
                                Console.WriteLine("---> user command called");
                            ftp.prompForLogin();
                            break;

                        case MKDIR:
                            if (ftp.inDebugMode)
                                Console.WriteLine("---> mkdir command called");
                            ftp.handleMKDCmd(argv);
                            break;
   
                        default:
                            if (ftp.inDebugMode)
                                Console.WriteLine("---> unrecognized command called");
                            Console.WriteLine("Invalid command");
                            break;
                    }
                }
            } while (!eof);
        }
    }
}