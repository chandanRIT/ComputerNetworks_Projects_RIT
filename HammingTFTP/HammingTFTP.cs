using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

    /**
     * This class represents the HammingFTTP client and has methods to request a file to download from the Hamming Server
     * @author Kumar Chandan
     */
    class HammingTFTP
    {
        static int PORT_NUMBER = 7000;
        static byte[] OP_NO_ERROR = { 0, 1 }, OP_ERROR = { 0, 2 };
        static byte STR_TERMINATOR = 0;

        private string fileName;
        private UdpClient tftpClient;

        /**
         * The 'main' method
         * @param name="args" : Command-line arguments
         */
        static void Main(string[] args)
        {
            if (!checkArgs(args))
                return;

            byte[] opcode_RPQ = args[0].Equals("error") ? OP_ERROR : OP_NO_ERROR;
            HammingTFTP hamTFTP = new HammingTFTP(opcode_RPQ, args[1], args[2]);
            hamTFTP.downloadFile();
        }


        /**
         * Constructor
         * @param name="fileName" : requested file
         * @param name="opcode_RPQ" : opcode to send in the request
         * @param name="tftpHost" : host name of the server
         */
        public HammingTFTP(byte[] opcode_RPQ, string tftpHost, string fileName)
        {
            this.fileName = fileName;
            List<byte> msgInBytesList = new List<Byte>();
            //sets up the message format for request from the client (opcode = 01)
            msgInBytesList.AddRange(opcode_RPQ);
            msgInBytesList.AddRange(Encoding.UTF8.GetBytes(fileName)); // args[2] = filename
            msgInBytesList.Add(STR_TERMINATOR);
            msgInBytesList.AddRange(Encoding.UTF8.GetBytes("netascii")); // args[0] = transfer mode [netascii/octet]
            msgInBytesList.Add(STR_TERMINATOR);

            //sets up the client's connection to the TFTP server and sends the message in its specified format        
            try
            {
                tftpClient = new UdpClient(tftpHost, PORT_NUMBER);
                tftpClient.Send(msgInBytesList.ToArray(), msgInBytesList.Count); // ags[1] = tftp-host 
            }
            catch (SocketException se)
            {
                Console.WriteLine("Error connecting to the server : " + tftpHost + " (" + se.GetType() + ")");
                if (tftpClient != null)
                    tftpClient.Close();
                //Console.ReadKey();
                return;
            }
        }

        /**
         * checks the command-line arguments for any discrepencies from the input specs
         * @returns true if the args are correct to proceed, else false
         */
        private static bool checkArgs(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage : [mono] HammingTFTP.exe [error|noerror] tftp-host file");
                //Console.ReadKey();
                return false;
            }

            if (!(args[0].ToLower().Equals("error") || args[0].ToLower().Equals("noerror")))
            {
                Console.WriteLine("Error in second argument. The 2nd argument can only be either 'error' or 'noerror'");
                //Console.ReadKey();
                return false;
            }

            return true;
        }

        /**
         * closes any open connections 
         */
       private void closeLoseEnds(FileStream fileStream, bool errorCase = true){
            fileStream.Close();
            if(errorCase)
                File.Delete(fileName);

            tftpClient.Close();
            //Console.ReadKey();
        }

        /**
         * downloads the requested file from the Hamming Server.
         */
        private void downloadFile()
        {
            //int startCount = 0;
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
            FileStream fileStream = File.Open(fileName, FileMode.Create);
            Console.Write("Downloading file");
           
            while (true)
            {
                byte[] response = null;
                try
                {
                    response = tftpClient.Receive(ref ipEndPoint);
                }
                catch (SocketException se)
                { //if connection breaks when receiving
                    Console.WriteLine("Error occured while receiving packet " + se.GetType());
                    closeLoseEnds(fileStream);
                    return;
                }

                List<byte> responseAsList = new List<byte>(response);

                // if the packet from server has "03" (the opcode) in its first 2 bytes (it means a data packet)
                if (response[0] == 0 && response[1] == 3)
                {
                    //send back acknowledgement ([0-4][<block-number>], total 4 bytes)
                    
                    if (response.Length < 516) //if encountered EOF
                    {
                        if (response.Length > 4)
                        {
                            //Console.WriteLine(Encoding.UTF8.GetString(responseAsList.GetRange(4, extractedDataAsList.Count).ToArray()));
                            //add the data in the final packet to the buffer
                            List<byte> decodedData = decode(responseAsList.GetRange(4, response.Length - 4));
                            if (decodedData == null)
                            {
                                byte[] nackFromClient = { 0, 6, response[2], response[3] };
                                tftpClient.Send(nackFromClient, 4); // ags[1] = tftp-host 
                                continue;
                            }
                            fileStream.Write(decodedData.ToArray(), 0, decodedData.Count);
                        }
                        else {
                            if (response[2] == 0 && response[3] == 1)
                            {
                                fileStream.Close();
                                File.Delete(fileName);
                                tftpClient.Close();
                                return;
                            }
                        }
                        break;
                    }
                    else
                    {
                        List<byte> decodedData = decode(responseAsList.GetRange(4, 512));
                        if (decodedData == null) {
                            byte[] nackFromClient = { 0, 6, response[2], response[3] };
                            tftpClient.Send(nackFromClient, 4); // ags[1] = tftp-host 
                            continue;
                        }
                        fileStream.Write(decodedData.ToArray(), 0, decodedData.Count);
                    }

                    byte[] ackFromClient = { 0, 4, response[2], response[3] };
                    tftpClient.Send(ackFromClient, 4); // ags[1] = tftp-host 

                    //if (startCount % 1 == 0)
                        Console.Write(" *");
                    //startCount++;
                }

                // Error packet sent by server, contains [opcode-2B, errorNumber-2B, errorMessage, stringTerminator-1B]
                else if (response[0] == 0 && response[1] == 5)
                {
                    handleErrorPacket(response, responseAsList);
                    closeLoseEnds(fileStream);
                    return;
                }

                //handling packets with unrecognized opCode    
                else
                {
                    Console.WriteLine("packet containing unrecognized opcode received");
                    closeLoseEnds(fileStream);
                    return;
                }

            }

            //write all the extracted data in the byte Array to the file
            //File.WriteAllBytes(fileName, dataToWriteToFile.ToArray());
            Console.WriteLine("\nFile downloaded: " + fileName);
            closeLoseEnds(fileStream, false);
        }

        /**
         * responsible for handling error Packets sent by the server
         */
       private void handleErrorPacket(byte[] response, List<byte> responseAsList)
        {
            //Console.WriteLine("opcode received : Error (05)");
            string errorNumberAsString = "" + response[2] + response[3];
            string errorMessage = Encoding.UTF8.GetString(responseAsList.GetRange(4, response.Length - 1 - 4).ToArray());

            Console.WriteLine();
            Console.WriteLine("Error Occured while downloading '" + fileName +
                "' : Error Code " + int.Parse(errorNumberAsString) + ": " + errorMessage);
        }

        /**
         * returns subrange of the input bit array
         * @param name="bitArr" : input BitArray
         * @param name="high" : index (inclusive) where to end 
         * @param name="low" : index (inclusive) where to start 
         * @returns subrange of the input bit array
         */
        public static bool[] getSubBitArray(BitArray bitArr, int low, int high)
        {
            bool[] boolArr = new bool[high - low + 1];
            for (int i = low; i <= high; i++)
                boolArr[i - low] = bitArr[i];
            return boolArr;
        }

        /**
         * converts from bitArray to List of Bytes
         * @param name="bitArray" : input BitArray
         * @returns List of bytes
         */
        public static List<byte> convertToByteList(BitArray bitArray)
        {
            byte[] byteArray = new byte[(int)Math.Ceiling((double)bitArray.Length / 8)];
            bitArray.CopyTo(byteArray, 0);
            return new List<byte>(byteArray);
        }

        /**
         * decodes the passed datablock by doing Hamming check and extracting out the interleaved hamming bits
         * @returns decoded list of bytes
         */
        private static List<byte> decode(List<byte> dataBlock)
        {
            List<byte> decodedBytes = new List<byte>();
            List<bool> prevLeftOverBits = new List<bool>();

            //work on every 4 bytes of data
            for (int count = 0; count < dataBlock.Count / 4; count++)
            {
                List<byte> fourByteBlock = dataBlock.GetRange(count * 4, 4);
                // fourByteBlock.Reverse();
              
                BitArray bitArray = new BitArray(fourByteBlock.ToArray());
               
                //do hamming check here 
                int result = checkHamming(bitArray);
                if (result == -2) {
                    return null;
                }

                //remove the hamming bits
                List<bool> boolList = new List<bool>();
                boolList.Add(bitArray[2]);
                boolList.AddRange(HammingTFTP.getSubBitArray(bitArray, 4, 6));
                boolList.AddRange(HammingTFTP.getSubBitArray(bitArray, 8, 14));
                boolList.AddRange(HammingTFTP.getSubBitArray(bitArray, 16, 30));
                
                //attach previous left over bits to the start
                boolList.InsertRange(0, prevLeftOverBits);

                List<byte> extractedByteBlock;
                if (boolList.Count != 32)
                {
                    extractedByteBlock = convertToByteList(new BitArray(boolList.GetRange(0, 24).ToArray()));
                    prevLeftOverBits.Clear();
                    prevLeftOverBits.AddRange(boolList.GetRange(24, boolList.Count - 24));
                }
                else
                {
                    extractedByteBlock = convertToByteList(new BitArray(boolList.GetRange(0, 32).ToArray()));
                    prevLeftOverBits.Clear();
                }

                //removing null bytes at the end of the stream
                if (count == dataBlock.Count / 4 - 1)
                {
                    int nullIndex = extractedByteBlock.IndexOf(0);
                    if(nullIndex != -1)
                        extractedByteBlock = extractedByteBlock.GetRange(0, nullIndex);
                }
                decodedBytes.AddRange(extractedByteBlock);
            }
            return decodedBytes;
        }

        /**
         * checks hamming positions and corrects single bit error and detects double bit errors
         * @param name="bitArray" : hamming encoded bit array
         * @returns index of the hamming bit which was wrong, '-2' for 2-bit errors, '-1' for no errors
         */
       private static int checkHamming(BitArray bitArray)
        {
            int wrongParitiesSum = 0;
            for (int i = 0; i < 5; i++) { //check 1s, 2s, 4s, 8s, 16s bit positions
                List<bool> parityCheckList = new List<bool>();
                int hammingStart = (int)Math.Pow(2, i);
                for(int j=hammingStart; j<bitArray.Count; j+=2*hammingStart){
                    parityCheckList.AddRange(HammingTFTP.getSubBitArray(bitArray, j-1, j-1 + hammingStart-1));
                }
                wrongParitiesSum += checkEvenParity(parityCheckList.GetEnumerator()) ? 0 : hammingStart;
            }

            if (wrongParitiesSum != 0) // if there is an error in the first 31 bits, then correct that error
            {
                bitArray[wrongParitiesSum - 1] = !bitArray[wrongParitiesSum - 1];
                if (checkEvenParity(bitArray.GetEnumerator()))
                    return wrongParitiesSum - 1; // then its a bit-error, thus return just the error index
                else
                    return -2; //its a 2-bit error
            }
            //if (wrongParitiesSum == 0) //parities checkout 
            return checkEvenParity(bitArray.GetEnumerator()) ? -1 : 31; // -1 for no error, 31 is the index of the over-all parity, which is in error.
        }

        /**
         * checks for even parity
         * @param name="iterator" : iterator of the list/BitArray for which the parity is to be checked
         * @returns : true if passes the even parity, false if fails the check
         */
        public static bool checkEvenParity(IEnumerator iterator){
            int count = 0;
            while(iterator.MoveNext()){
                if((bool)iterator.Current)
                    count++;
            }
            return count%2 == 0;
        }
    }
