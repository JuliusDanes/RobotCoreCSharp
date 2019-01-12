using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RobotCore {
    class Program {
        static TcpListener listener { get; set; }
        static Dictionary<string, dynamic> _socketDict = new Dictionary<string, dynamic> ();
        static Dictionary<string, Thread> gotoDict = new Dictionary<string, Thread> ();
        private static int[] _posXYZ = { 0, 0, 0 };
        static string useAs, myIP;

        static void Main (string[] args) {
            try {
                Console.Write ("Use As: ");
                useAs = Console.ReadLine ();
                startServer (GetIPAddress (), 8686); // Start the server  
                new Thread (setCommand).Start ();
            } catch (System.Exception e) { }
        }

        static int[] posXYZ {
            get {
                return _posXYZ;
            }
            set {
                _posXYZ = value;
                Console.WriteLine ("# " + ("X:" + value[0] + " Y:" + value[1] + " ∠:" + value[2] + "°"));
            }
        }

        private static void setCommand () {
            try {
                while (true) {
                    string Command = Console.ReadLine ();
                    if (Command.ToLower () == "quit") {
                        listener.Stop ();
                        listener = null;
                    } else if (Command.ToLower () == "key")
                        keyEvent ();
                    else if (_socketDict.ContainsKey ("BaseStation"))
                        ResponeCallback (_socketDict["BaseStation"], Command);
                    else
                        Console.WriteLine ("!...Not Connected...!");
                }
            } catch (System.Exception e) {
                Console.WriteLine ("% setCommand error \n~\n" + e);
            }
        }

        private static void keyEvent () {
            ConsoleKeyInfo _keyPress;
            do {
                int[] _temp = posXYZ.ToArray ();
                _keyPress = Console.ReadKey ();
                if (_keyPress.Key == ConsoleKey.RightArrow)
                    posXYZ[0] += 1;
                else if (_keyPress.Key == ConsoleKey.LeftArrow)
                    posXYZ[0] -= 1;
                else if (_keyPress.Key == ConsoleKey.UpArrow)
                    posXYZ[1] -= 1;
                else if (_keyPress.Key == ConsoleKey.DownArrow)
                    posXYZ[1] += 1;
                else if (_keyPress.Key == ConsoleKey.PageUp)
                    posXYZ[2] += 1;
                else if (_keyPress.Key == ConsoleKey.PageDown)
                    posXYZ[2] -= 1;

                if ((posXYZ != _temp) && (_socketDict.ContainsKey ("BaseStation")))
                    SendCallBack (_socketDict["BaseStation"], (String.Join (",", posXYZ)));
            } while (_keyPress.Key != ConsoleKey.Escape);
        }

        private static void GotoLoc (string Robot, int endX, int endY, int endAngle, int shiftX, int shiftY, int shiftAngle) {
            try {
                Console.WriteLine ("# " + Robot + " : Goto >> " + ("X:" + endX + " Y:" + endY + " ∠:" + endAngle + "°"));
                bool[] chk = { true, true, true };
                while (chk[0] |= chk[1] |= chk[2]) {
                    if (posXYZ[0] > 12000)
                        posXYZ[0] = int.Parse (posXYZ[0].ToString ().Substring (0, 4));
                    if (posXYZ[1] > 9000)
                        posXYZ[1] = int.Parse (posXYZ[1].ToString ().Substring (0, 4));
                    if (posXYZ[2] > 360)
                        posXYZ[2] = int.Parse (posXYZ[2].ToString ().Substring (0, 2));

                    if ((posXYZ[0] > endX) && (shiftX > 0))
                        shiftX *= -1;
                    else if ((posXYZ[0] < endX) && (shiftX < 0))
                        shiftX *= -1;
                    if ((posXYZ[1] > endY) && (shiftY > 0))
                        shiftY *= -1;
                    else if ((posXYZ[1] < endY) && (shiftY < 0))
                        shiftY *= -1;
                    if ((posXYZ[2] > endAngle) && (shiftAngle > 0))
                        shiftAngle *= -1;
                    else if ((posXYZ[2] < endAngle) && (shiftAngle < 0))
                        shiftAngle *= -1;

                    if (posXYZ[0] != endX) {
                        if (Math.Abs (endX - posXYZ[0]) < Math.Abs (shiftX)) // Shift not corresponding
                            shiftX = (endX - posXYZ[0]);
                        posXYZ[0] += shiftX; // On process
                    } else
                        chk[0] = false; // Done
                    if (posXYZ[1] != endY) {
                        if (Math.Abs (endY - posXYZ[1]) < Math.Abs (shiftY)) // Shift not corresponding
                            shiftY = (endY - posXYZ[1]);
                        posXYZ[1] += shiftY; // On process
                    } else
                        chk[1] = false; // Done
                    if (posXYZ[2] != endAngle) {
                        if (Math.Abs (endAngle - posXYZ[2]) < Math.Abs (shiftAngle)) // Shift not corresponding
                            shiftAngle = (endAngle - posXYZ[2]);
                        posXYZ[2] += shiftAngle; // On process
                    } else
                        chk[2] = false; // Done

                    posXYZ = posXYZ;
                    SendCallBack (_socketDict["BaseStation"], (string.Join (",", posXYZ)), "Goto");
                    Thread.Sleep (100); // time per limit
                }
            } catch (Exception e) { }
        }

        private static void threadGoto (string keyName, Thread th) {
            if (gotoDict.ContainsKey (keyName)) {
                gotoDict[keyName].DisableComObjectEagerCleanup ();
                gotoDict.Remove (keyName);
            }
            gotoDict.Add (keyName, th);
            gotoDict[keyName].Start ();
        }

        private static string GetIPAddress () {
            using (Socket socket = new Socket (AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
                socket.Connect ("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                myIP = endPoint.Address.ToString ();
            }
            return myIP;
        }

        static string socketToIP (dynamic socket) {
            return (socket.Client.RemoteEndPoint.ToString ().Split (':')) [0];
        }

        static string socketToName (dynamic socket) {
            dynamic[] arr = { "BaseStation", "RefereeBox", "Robot1", "Robot2", "Robot3" };
            for (int i = 0; i < arr.Length; i++)
                if ((_socketDict.ContainsKey (arr

                        [i])) && (_socketDict[arr[i]].Client.RemoteEndPoint == socket.Client.RemoteEndPoint))
                    return arr[i];
            return socket.Client.RemoteEndPoint.ToString ();
        }

        public static void startServer (string IP, int port) {
            try {
                IPAddress IPadd = IPAddress.Parse (IP);
                listener = new TcpListener (IPadd, port);
                listener.Start ();
                Console.WriteLine ($"Server started. Listening to TCP clients at {IP}:{port}");
                new Thread (listenClient).Start (); // Start listening.
            } catch (System.Exception e) {
                Console.WriteLine ("% startServer error \n~\n" + e);
            }
        }

        public static void listenClient () {
            try {
                while (listener != null) {
                    Console.WriteLine ("Waiting for client...");
                    var clientTask = listener.AcceptTcpClientAsync (); // Get the client
                    var socket = clientTask.Result;
                    if (socket != null) {
                        _socketDict.Add ("BaseStation", socket);
                        new Thread (() => ReceiveCallBack (socket)).Start ();
                        // new Thread(() => SendCallBack(socket)).Start(); 
                    }
                }
            } catch (System.Exception e) {
                Console.WriteLine ("% listenClient error \n" /*+ e*/ );
            }
        }

        public static void ReceiveCallBack (dynamic socket) {
            try {
                string message = string.Empty;
                Console.WriteLine ("Client connected. Waiting for data.");
                while ((listener != null) && (socket != null) && (message != null && !message.StartsWith ("quit"))) {
                    byte[] buffer = new byte[1024];
                    socket.GetStream ().Read (buffer, 0, buffer.Count ());
                    message = Encoding.ASCII.GetString (buffer).Trim ();
                    message = String.Join ("", message.Where (i => !char.IsControl (i)));
                    ResponeCallback (socket, message);
                }
                Console.WriteLine ("Closing connection.");
                socket.GetStream ().Close ();
            } catch (System.Exception e) {
                Console.WriteLine ("% Retrieve error \n~\n" + e);
            }
        }

        public static void SendCallBack (dynamic socket, string txtMessage) {
            try {
                if ((listener != null) && (socket != null) && (!string.IsNullOrWhiteSpace (txtMessage)) && (txtMessage != "quit")) {
                    byte[] buffer = new byte[1024];
                    buffer = System.Text.Encoding.ASCII.GetBytes (txtMessage);
                    socket.GetStream ().Write (buffer, 0, buffer.Length);

                    string output = "@ " + socketToName (socket) + " : " + txtMessage;
                    Console.WriteLine (output);
                }
            } catch (System.Exception e) {
                Console.WriteLine ("% Send error \n~\n" + e);
            }
        }

        public static void SendCallBack (dynamic socket, string txtMessage, string Goto) {
            try {
                if ((listener != null) && (socket != null) && (!string.IsNullOrWhiteSpace (txtMessage)) && (txtMessage != "quit")) {
                    byte[] buffer = new byte[1024];
                    buffer = System.Text.Encoding.ASCII.GetBytes (txtMessage);
                    socket.GetStream ().Write (buffer, 0, buffer.Length);
                }
            } catch (System.Exception e) {
                Console.WriteLine ("% Send error \n~\n" + e);
            }
        }

        private static string ResponeCallback (dynamic socket, string message) {
            string respone = string.Empty;
            var _dtMessage = message.Split ('|');
            if ((!_dtMessage[0].StartsWith ("go")) && (Regex.IsMatch (_dtMessage[0], "[-]{0,1}[0-9]{1,4},[-]{0,1}[0-9]{1,4},[-]{0,1}[0-9]{1,4}"))) {
                // If message is data X & Y from encoder
                /// Scale is 1 : 20 
                dynamic[] msgXYZ = _dtMessage[0].Split (',');
                msgXYZ = msgXYZ.Where (item => (!string.IsNullOrWhiteSpace (item))).ToArray ();
                _dtMessage[0] = string.Empty;
                if (msgXYZ.Length > 3) // If data receive multi value X & Y (error bug problem)
                {
                    msgXYZ[0] = msgXYZ[msgXYZ.Length - 3].Substring (msgXYZ[msgXYZ.Length - 1].Length);
                    msgXYZ[1] = msgXYZ[msgXYZ.Length - 2];
                    msgXYZ[2] = msgXYZ[msgXYZ.Length - 1];
                }

                if ((!string.IsNullOrWhiteSpace (msgXYZ[0])) && (Convert.ToInt64 (msgXYZ[0]) > 12000))
                    msgXYZ[0] = msgXYZ[0].ToString ().Substring (0, 4);
                if ((!string.IsNullOrWhiteSpace (msgXYZ[1])) && (Convert.ToInt64 (msgXYZ[1]) > 9000))
                    msgXYZ[1] = msgXYZ[1].ToString ().Substring (0, 4);
                if ((!string.IsNullOrWhiteSpace (msgXYZ[2])) && (Convert.ToInt64 (msgXYZ[2]) > 360))
                    msgXYZ[2] = msgXYZ[2].ToString ().Substring (0, 2);

                for (int i = 0; i < msgXYZ.Length; i++)
                    posXYZ[i] = Convert.ToInt32 (msgXYZ[i]);

                _dtMessage[0] = "X:" + posXYZ[0] + " Y:" + posXYZ[1] + " ∠:" + posXYZ[2] + "°";
            } else if (Regex.IsMatch (_dtMessage[0], "go[-]{0,1}[0-9]{1,4},[-]{0,1}[0-9]{1,4},[-]{0,1}[0-9]{1,4}")) {
                var dtXYZ = _dtMessage[0].Substring (2).Split (',').Select (item => int.Parse (item)).ToArray ();
                threadGoto ("Robot", new Thread (obj => GotoLoc (useAs, dtXYZ[0], dtXYZ[1], dtXYZ[2], 20, 20, 1)));
            } else if ((_socketDict.ContainsKey ("BaseStation")) && (socket.Client.RemoteEndPoint.ToString ().Contains (_socketDict["BaseStation"].Client.RemoteEndPoint.ToString ())))
            // else if (true)
            {
                // If socket is Base Station socket
                switch (_dtMessage[0]) {
                    /// INFORMATION ///
                    case "B": //Get the ball
                        respone = "Ball on " + socketToName (socket);
                        goto broadcast;

                        /// OTHERS ///
                    case "get_time": //TIME NOW
                        respone = DateTime.Now.ToLongTimeString ();
                        goto multicast;
                    default:
                        //Console.WriteLine("# Invalid Command :<");
                        break;
                }
            }
            goto end;

            broadcast:
                SendCallBack (_socketDict["BaseStation"], respone + "|" + "Robot1,Robot2,Robot3");
            // sendByHostList("BaseStation", respone + "|" + "Robot1,Robot2,Robot3");
            goto end;

            multicast:
                if (_dtMessage.Length > 1)
                    SendCallBack (_socketDict["BaseStation"], respone + "|" + _dtMessage[1]);
                else
                    SendCallBack (_socketDict["BaseStation"], respone);
            // sendByHostList("BaseStation", respone + "|" + chkRobotCollect);
            goto end;

            end:
                if (!string.IsNullOrWhiteSpace (_dtMessage[0]))
                    Console.WriteLine ("> " + socketToName (socket) + " : " + _dtMessage[0]);
            return respone;
        }
    }
}