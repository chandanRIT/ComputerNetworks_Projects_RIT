using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace TFTP
{   
    /**
     * The TFTPreader class that downloads the required file from the specified server.
     * @author Kumar Chandan
     */
    class TFTPreader
    {
        static int PORT_NUMBER = 6969;
        static byte[] OP_RPQ = { 0, 1 };
        static byte STR_TERMINATOR = 0;

        /**
         * The 'main' method
         * @param name="args" : Command-line arguments
         */
        static void Main(string[] args)
        {
            if (args.Length != 3) {
                Console.WriteLine("Usage : [mono] TFTPreader.exe [netascii | octet] tftp-host file");
                //Console.ReadKey();
                return;
            }

            string transferMode = args[0], tftpHost = args[1], fileName = args[2];
            List<byte> msgInBytesList = new List<Byte>();

            if (!(transferMode.ToLower().Equals("netascii") || transferMode.ToLower().Equals("octet"))) {
                Console.WriteLine("Error in second argument. The transfer mode can only be either 'netascii' or 'octet'");
                //Console.ReadKey();
                return;
            }

            //sets up the message format for request from the client (opcode = 01)
            msgInBytesList.AddRange(OP_RPQ);
            msgInBytesList.AddRange(Encoding.UTF8.GetBytes(fileName)); // args[2] = filename
            msgInBytesList.Add(STR_TERMINATOR);
            msgInBytesList.AddRange(Encoding.UTF8.GetBytes(transferMode)); // args[0] = transfer mode [netascii/octet]
            msgInBytesList.Add(STR_TERMINATOR);

            //sets up the client's connection to the TFTP server and sends the message in its specified format        
            UdpClient tftpClient;
            try
            {
                tftpClient = new UdpClient(tftpHost, PORT_NUMBER);
                tftpClient.Send(msgInBytesList.ToArray(), msgInBytesList.Count); // ags[1] = tftp-host 
            }
            catch (SocketException se) {
                Console.WriteLine("Error connecting to the server : " + tftpHost + " (" + se.GetType() + ")");
                //Console.ReadKey();
                return;
            }
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

            int i = 0;
            List<byte> dataToWriteToFile = new List<byte>();
            Console.Write("Downloading file");
            while(true){
                byte[] response = null;
                try
                {
                    response = tftpClient.Receive(ref RemoteIpEndPoint);
                }
                catch (SocketException se){ //if connection breaks when receiving
                    Console.WriteLine("Error occured while receiving packet " + se.GetType());
                    tftpClient.Close();
                    //Console.ReadKey();
                    return;
                }

                List<byte> responseAsList = new List<byte>(response);
                
                // if the packet from server has "03" (the opcode) in its first 2 bytes (it means a data packet)
                if (response[0] == 0 && response[1] == 3) { 
                    //send back acknowledgement ([0-4][<block-number>], total 4 bytes)
                    byte[] ackFromClient = { 0, 4, response[2], response[3] };
                    tftpClient.Send(ackFromClient, 4); // ags[1] = tftp-host 
                   
                    if (response.Length < 516) //if encountered EOF
                    {
                        if (response.Length > 4)
                        {   
                            //Console.WriteLine(Encoding.UTF8.GetString(responseAsList.GetRange(4, extractedDataAsList.Count).ToArray()));
                            //add the data in the final packet to the buffer
                            dataToWriteToFile.AddRange(responseAsList.GetRange(4, response.Length - 4));
                        }
                        break;
                    }
                    else {
                        dataToWriteToFile.AddRange(responseAsList.GetRange(4, 512));
                    }

                    if (i % 150 == 0)
                        Console.Write(" *");
                    i++;
                }

                // Error packet sent by server, contains [opcode-2B, errorNumber-2B, errorMessage, stringTerminator-1B]
                else if (response[0] == 0 && response[1] == 5) 
                {
                    //Console.WriteLine("opcode received : Error (05)");
                    string errorNumberAsString = "" + response[2] + response[3];
                    string errorMessage = Encoding.UTF8.GetString(responseAsList.GetRange(4, response.Length - 1 - 4).ToArray());
                    
                    Console.WriteLine();
                    Console.WriteLine("Error Occured while downloading '" + fileName + 
                        "' : Error Code " + int.Parse(errorNumberAsString) + ": " + errorMessage);
                    
                    tftpClient.Close();
                    //Console.ReadKey();
                    return;
                }

                //handling packets with unrecognized opCode    
                else {
                    Console.WriteLine("packet containing unrecognized opcode received");
                    tftpClient.Close();
                    //Console.ReadKey();
                    return;
                }

            }

            //write all the extracted data in the byte Array to the file
            File.WriteAllBytes(fileName, dataToWriteToFile.ToArray());
            Console.WriteLine("\nFile downloaded: " + fileName);
            tftpClient.Close();

            Console.ReadKey();
        }
    }
}
  