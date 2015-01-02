Computer-Networks Projects at RIT (Quarter: Spring 2013)
========================================================

These folders contain code for all the mini-projects I worked on during the course "Data Communication And Networks" (Spring 2013).
The project PDFs are in the respective subdirectories. They explain in detail about each of the project requirements and its description.

A brief abstract of each of the projects is provided below, further detals are in the PDF of the respective folder.: 

FTP:
---
For this project, write an RFC959 compliant FTP client. 
The only allowed deviation from the RFC is that your client only needs to provide ¨file-structure¨transfers (directory transfers need not be supported). 
The program can provide either a text based and/or graphical user interface.
The program should read commands from the user, execute them, and display messages that indicate their success/progress or failure. Implement each of the commands in the table provided in project-details.pdf
to get full credit. You may, if you wish, implement additional commands. Note any files transferred to/from the server will be placed in/copied from the current working directory.

HammingTFTP:
---
This project implements a a slightly advanced TFTP version with Hamming error detection and correction for 1 bit errors.


TFTP:
---
Write a program named, TFTPreader, that will read a file from a standard TFTP server.
The program will take three command line arguments: the transfer mode to use, the name of the host
on which the TFTP server is running, and the name of the file to transfer (the TFTP server is located
at the well-known port 69). If the program is not invoked correctly (i.e. wrong number of arguments,
or an invalid machine name), an appropriate error message will be printed and the program terminates
with generating any additional output.
The program will then connect to the TFTP server and transfer the specified file. If the transfer
is successful, a file with the name specified in the command line will be created in the directory in which
the program was started. The file will contain the data that was transferred from the TFTP server.
If in the course of the transfer, the server sends an error packet, the program should print a message
indicating the error number and error message sent in the error packet. After printing this information
the program will terminate without producing any additional output.

LogServer:
---
Part 1: Write a class named LogClient that implements the ILogService (LogService in Java)
interface. This class can be used by a client to access the distributed logging system. All of the details
regarding communication with the log server are encapsulated in the LogClient class. 

Part 2: Write a server for the distributed logging service. Your server must implement the exact same
protocol that my server implements. It will be tested by running a test client against your running server. 
The server should record all log messages to a file named LogServer.log.
It also must log debugging information in a separate file as well. The exact format of the file(s),
and their contents are left up to you to decide. Minimally, the file containing the log messages must
contain every message logged by every client and the ticket number that was used to log the message.
It might be useful to look at the log files created by my server for guidance. Note that the
messages associated with an open ticket should be stored in memory for quick retrieval
(in other words the file that contains copies of the log messages is for archival purposes
only).


