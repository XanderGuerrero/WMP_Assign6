//NAME:	        Client.cs
//PROJECT:      Assignment6
//DATE:	        04/11/2014
//AUTHORS:	    Alex Guerrero, Manbir Singh
//DESCRIPTION:	The Client side program attempts to establish conncetions with a server host by using the IP 
//              address supplied by the user at the start of the program.  If the program is successful in 
//              connecting, a write and read thread is created to handle messaging between the two programs.
//              When the user chooses to exit both the server and client shut down communications
//              HOW TO RUN:  client cmd line arguments are ip of server and chat name
//              




//references
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;




//CLASS
//-------------------------
//NAME	:	Client
//PURPOSE :	This object contains sockets, threads and variables as data members to establish a chat connection over a network
//-------------------------
public class Client
{
    //class Variables
    public static Socket sender;
    public static Thread write;
    public static Thread read;
    public static string clientName;
    public static string serverName;
    public static List<string> outputArea = new List<string>();
    static int areaHeights = 0;
    static bool firstSend = true;
    static bool firstReceive = true;




    /* ---------------------------
    *	Name	:   StartClient
    *
    *	Purpose :   StartClient tries to establish a conncetion to a host computer
    *	            by using the port 11000 and the client entered IP address of the host.
    *	           
    *
    *	Inputs	:	NONE
    *	
    *	Outputs	:	Program output is placed into a list and the contents of the list are displayed to the
    *               screen
    *               
    *	Returns	:	NONE
    */
    public static void StartClient(string ipaddress, string userName)
    {
        //place the userName into the client name variable
        clientName = userName;

        //determine the window height  minus 2
        areaHeights = (Console.WindowHeight - 2);
        drawScreen();

        // Data buffer for incoming data.
        // Connect to a remote device. if an error occurs
        // catch them
        try
        {
            // Establish the remote endpoint for the socket.
            // This example uses port 11000 on the local computer.
            IPHostEntry ipHostInfo = Dns.Resolve(ipaddress);
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP  socket.
            sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Connect the socket to the remote endpoint. Catch any errors.
            try
            {
                try
                {
                    //if connect fails, display error msg to screen in catch
                    //and exit
                    sender.Connect(remoteEP);
                }
                catch (Exception e)
                {
                    outputArea.Insert(0, e.Message);
                    drawScreen();
                    Console.ReadKey();
                    Environment.Exit(0);
                }

                //connection has been made, inform user and start the write and read threads
                outputArea.Insert(0, "Connected to the server, enter *esc to disconnect...");
                drawScreen();
                write = new Thread(new ThreadStart(WriteToServer));
                write.Start();
                read = new Thread(new ThreadStart(ReadFromServer));
                read.Start();
            }
            //display the cmd line argument expection error msg
            catch (ArgumentNullException ane)
            {
                outputArea.Insert(0, ane.ToString());
                drawScreen();
            }
            //display the socket expection error msg
            catch (SocketException se)
            {
                outputArea.Insert(0, se.ToString());
                drawScreen();
            }
            //display the expection error msg
            catch (Exception e)
            {
                outputArea.Insert(0, e.ToString());
                drawScreen();
            }
        }
        //Display error
        catch (Exception e)
        {
            outputArea.Insert(0, e.ToString());
            drawScreen();
        }
    }




    /* ---------------------------
    *	Name	:   WriteToServer
    *
    *	Purpose :   Writes user entered data from the keyboard into a buffer and sends it to the server.  
    *	            A first send messages sends the client username.  If the user chooses to exit with 
    *	            the "*esc" request, the client shuts down and sends notice to the server
    *
    *	Inputs	:	NONE
    *	
    *	Outputs	:	Program output is placed into a list and the contents of the list are displayed to the
    *               screen by a call to drawScreen
    *               
    *	Returns	:	NONE
    */
    public static void WriteToServer()
    {
        string data = null;
        byte[] msg;
        int bytesSent;

        //if this is the first send,
        if (firstSend == true)
        {
            //place the client name in the data buffer
            data = clientName;

            // Encode the data string into a byte array.
            msg = Encoding.ASCII.GetBytes(data);

            // Send the data through the socket.
            //set firstSend to false to never execute this
            //section of code
            bytesSent = sender.Send(msg);
            firstSend = false;
            data = null;
        }

        //constantly write
        while (true)
        {
            //prepare string to send to server
            data = null;
            data = clientName + ": ";
            data += Console.ReadLine();

            try
            {
                //add the client entered data to the list and call drawScreen to update the screen
                outputArea.Insert(0, data);
                drawScreen();

                //if the user requests the *esc command to quit the program
                //inform the server and abort the read thread
                if (data == clientName + ": " + "*esc")
                {
                    // Encode the data string into a byte array.
                    msg = Encoding.ASCII.GetBytes(data);
                    // Send the data through the socket.
                    bytesSent = sender.Send(msg);
                    read.Abort();
                    data = null;
                    break;
                }

                // Encode the data string into a byte array.
                msg = Encoding.ASCII.GetBytes(data);

                // Send the data through the socket.
                bytesSent = sender.Send(msg);
            }
            //display cmd line argument error
            catch (ArgumentNullException ane)
            {
                outputArea.Insert(0, ane.ToString());
                drawScreen();
            }
            //display socket error
            catch (SocketException se)
            {
                outputArea.Insert(0, se.ToString());
                drawScreen();
            }
            //display expection error
            catch (Exception e)
            {
                outputArea.Insert(0, e.ToString());
                drawScreen();
            }
        }
    }




    /* ---------------------------
    *	Name	:   ReadFromServer
    *
    *	Purpose :   Thread used to read data from the server continually.  If it is the first receive, 
    *	            get the servers username for reference in the chat.  if the user enters "*esc", 
    *	            close communications and close thread
    *
    *	Inputs	:	NONE
    *	
    *	Outputs	:	Program output is placed into a list and the contents of the list are displayed to the
    *               screen
    *               
    *	Returns	:	NONE
    */
    public static void ReadFromServer()
    {
        byte[] bytes = new byte[1024];
        int bytesRec;
        string data;

        //if first msg received, get the data,
        //decode and use as the servers username
        //set firstReceive to false
        if (firstReceive == true)
        {
            bytes = new byte[1024];
            bytesRec = sender.Receive(bytes);
            data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
            serverName = data;
            data = null;
            firstReceive = false;
        }

        //constantly read from server
        while (true)
        {
            //Receive the response from the remote device.
            //if error occurs, display error to user
            try
            {
                //receive and decode and place into data
                bytesRec = sender.Receive(bytes);
                outputArea.Insert(0, Encoding.ASCII.GetString(bytes, 0, bytesRec));
                data = Encoding.ASCII.GetString(bytes, 0, bytesRec);

                //id data is the *esc request,
                //close connections, set firstReceive and FirstSend as true 
                //and interrupt the write thread to call abort().
                //exit the application
                if (data == serverName + ": " + "*esc")
                {
                    sender.Close();
                    firstReceive = true;
                    firstSend = true;
                    write.Interrupt();
                    write.Abort();
                    Environment.Exit(0);
                }
                drawScreen();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
        if (sender != null)
        {
            Console.Write(clientName + ": ");
        }
    }




    /* ---------------------------
   *	Name	:   Main
   *
   *	Purpose :   Drives the chat program 
   *
   *	Inputs	:	Two command line arguments representing the IP address of the host 
   *                computer to establish a connection and a users desired username.
   *	
   *	Outputs	:	Error message if user does not enter a username. Text color is green
   *	Returns	:	NONE
   */
    public static int Main(String[] args)
    {
        try
        {
            //place commandline arguments into ip address and
            //username variable respectively.  Change the test color to green
            string ipaddress = args[0];
            string userName = args[1];
            Console.ForegroundColor = ConsoleColor.Green;

            //if the client entered an IP address,
            //call StartListening method to attempt communications
            if (ipaddress != null)
            {
                StartClient(ipaddress, userName);
            }
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