//NAME:	        Server.cs
//PROJECT:      Assignment6
//DATE:	        04/11/2014
//AUTHORS:	    Alex Guerrero, Manbir Singh
//DESCRIPTION:	The server program is used as one side of a chat program that also acts as the host computer for 
//              communications over a network.  The server program establishes threads to read and write and commands to
//              exit when the users choose to do so.
//              HOW TO RUN:  server cmd line arguments required are a chat name, ip address is localhost




//references
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;




//CLASS
//-------------------------
//NAME	:	Server
//PURPOSE :	This object contains sockets, threads and variables as data members to establish a chat connection over a network
//-------------------------
public class Server
{
    // Incoming data from the client.
    public static string data = null;
    public static Socket listener;
    public static Socket handler;
    public static Thread write;
    public static Thread read;
    public static string serverName;
    public static List<string> outputArea = new List<string>();
    static int areaHeights = 0;
    static bool firstSend = true;
    static bool firstReceive = true;
    static string clientName;




    /* ---------------------------
    *	Name	:   StartListening
    *
    *	Purpose :   The method StartListening() is used to establish a connection to a client on port
    *               11000 and the host IP address of this computer. The method also starts the read and write threads
    *               responsible for handling the messaging between the client and server.  If a connection cannot be established 
    *               a catch will display the error to the user. 
    *
    *	Inputs	:	string userName representing the name that will be displayed on the screen for reference.
    *	
    *	Outputs	:	NONE
    *	Returns	:	NONE
    */
    public static void StartListening(string userName)
    {
        //place the username entered in the command line into serverName
        serverName = userName;

        //determine the window height  minus 2
        areaHeights = (Console.WindowHeight - 2);/// 1;
        drawScreen();

        // Establish the local endpoint for the socket.
        // Dns.GetHostName returns the name of the 
        // host running the application.
        IPHostEntry ipHostInfo = Dns.Resolve("127.0.0.1");
        IPAddress ipAddress = ipHostInfo.AddressList[0];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

        // Create a TCP/IP socket.
        listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Try bind the socket to the local endpoint and 
        // listen for incoming connections.  Start the read and write threads
        // and accept the connections.  If an error occurs, 
        // catch the error and display the error messaeg to the screen.
        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(10);

            read = new Thread(new ThreadStart(ReadFromClient));
            read.Start();
            write = new Thread(new ThreadStart(WriteToClient));
            write.Start();
        }
        catch (Exception e)
        {
            outputArea.Insert(0, e.ToString());
            drawScreen();
        }
    }




    /* ---------------------------
    *	Name	:   WriteToClient
    *
    *	Purpose :   Thread to write data to client.  The thread accepts user input using 
    *               Console.ReadLine().  The string is converted to bytes and sent to the client.
    *               If the server user enters *esc, the server closes the connection and aborts the 
    *               read tread to terminate the program.
    *
    *	Inputs	:	NONE
    *	
    *	Outputs	:	NONE
    *	Returns	:	NONE
    */
    public static void WriteToClient()
    {
        byte[] msg;
        int bytesSent;

        //be constantly ready to send a message
        while (true)
        {
            //if a connection is available
            if (handler != null)
            {
                //prepare string to include the servers username, 
                //then get a line of data from the user
                data = serverName + ": ";
                data += Console.ReadLine();

                //when the data is ready and connection is live
                if (handler != null && data != null)
                {
                    //display servers data to the screem
                    outputArea.Insert(0, data);
                    drawScreen();

                    //Send the data to the client 
                    msg = Encoding.ASCII.GetBytes(data);
                    handler.Send(msg);

                    //if the data to send is the "*esc" to exit the program,
                    //close the connection, abort the ReadFromClient thread and break;
                    if (data == serverName + ": " + "*esc")
                    {
                        handler.Close();
                        read.Abort();
                        break;
                    }
                    //clear the buffer
                    data = null;
                }
            }
        }
    }




    /* ---------------------------
    *	Name	:   ReadFromClient
    *
    *	Purpose :   Thread used to handle receiving data from client and 
    *	            displaying the received content to the screen by calling drawScreen().
    *	            Upon successful connection to a client, the server receives the clients
    *               username.
    *               
    *               When the client sends the *esc to exit the chat, the thread closes the 
    *               connection and ends the application.
    *
    *	Inputs	:	NONE
    *	
    *	Outputs	:	NONE
    *	Returns	:	NONE
    */
    public static void ReadFromClient()
    {
        byte[] bytes = new Byte[1024];

        while (true)
        {
            handler = null;
            //display message to the screen
            outputArea.Insert(0, "Waiting for a connection...");
            drawScreen();

            // Program is suspended while waiting for an incoming connection.
            //When a connection is made display connection message
            handler = listener.Accept();
            outputArea.Insert(0, "Connection has been made, enter *esc to disconnect...");
            drawScreen();

            //if this is the first receive, get the clients userName and place it into 
            //a static variable
            if (firstReceive == true)
            {
                //data = serverName;
                bytes = new byte[1024];
                //receive data, decode and place into buffer
                int bytesRec = handler.Receive(bytes);
                data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                clientName = data;

                //send the servers username and set firstReceieve bool to false
                //to avoid this section of code for the rest of the program
                data = serverName;
                //receive data, encode and place into msg to send
                byte[] msg = Encoding.ASCII.GetBytes(data);
                handler.Send(msg);
                data = null;
                firstReceive = false;
            }


            //constantly listen for messages
            while (true)
            {
                //process a message, receive data
                data = null;
                bytes = new byte[1024];
                int bytesRec = handler.Receive(bytes);
                data = Encoding.ASCII.GetString(bytes, 0, bytesRec);

                //if the buffer is not empty
                if (data != "")
                {
                    //place data into list and display to screen
                    outputArea.Insert(0, data);
                    drawScreen();

                    //the client enters "*esc", close all connections
                    //display new message to server user set firstReceive to false
                    //incase need to adjust program for multiple users.
                    //break from loop
                    if (data == clientName + ": " + "*esc")
                    {
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                        outputArea.Insert(0, "Connection has been terminated");
                        drawScreen();
                        //.................
                        write.Interrupt();
                        write.Abort();
                        Environment.Exit(0);
                        //........................
                        firstReceive = true;
                        firstSend = true;
                        break;
                    }
                }

            }
        }
    }




    /* ---------------------------
    *	Name	:   drawScreen
    *
    *	Purpose :   Splits the screen into two sections to display the chat contents and to 
    *	            show the user the data entered before sending.  This function is adapted from
    *	            http://stackoverflow.com/questions/3434346/split-a-console-in-two-parts-for-two-outputs
    *
    *	Inputs	:	NONE
    *	
    *	Outputs	:	Program output is placed into a list and the contents of the list are displayed to the
    *               screen
     *               
    *	Returns	:	NONE
    */
    private static void drawScreen()
    {
        //if the count of the list is the same as the height of the window,
        //remove string from specified index
        if (outputArea.Count == areaHeights)
        {
            outputArea.RemoveAt(areaHeights - 1);
        }

        //flush the stream and clear the cosole
        Console.Out.Flush();
        Console.Clear();

        // Draw the area divider "-" along the width of the console
        for (int i = 0; i < Console.BufferWidth; i++)
        {
            Console.SetCursorPosition(i, areaHeights);
            Console.Write('-');
        }

        int currentLine = areaHeights - 1;

        //write the data above the divide line
        for (int i = 0; i < outputArea.Count; i++)
        {
            Console.SetCursorPosition(0, currentLine - (i + 1));
            Console.WriteLine(outputArea[i]);
        }

        //display the user name, this area is used to show users entered data before
        //it is sent
        Console.SetCursorPosition(0, Console.WindowHeight - 1);
        if (handler != null)
        {
            Console.Write(serverName + ": ");
        }
    }




    /* ---------------------------
    *	Name	:   Main
    *
    *	Purpose :   Drives the chat program 
    *
    *	Inputs	:	One command line arguments representing the users username in the program
    *	
    *	Outputs	:	Error message if user does not enter a username. Text color is green
    *	Returns	:	NONE
    */
    public static int Main(String[] args)
    {
        try
        {
            //place command line argument in variable
            //call startlistening() and pass it the username
            //change the text color to green
            string userName = args[0];
            Console.ForegroundColor = ConsoleColor.Green;
            StartListening(userName);
        }
        catch (Exception ex)
        {
            //error message
            Console.WriteLine(ex.Message);
            Console.ReadKey();
        }
        return 0;
    }
}