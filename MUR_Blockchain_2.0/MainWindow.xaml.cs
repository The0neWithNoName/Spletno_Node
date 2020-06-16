using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Windows.Threading;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http;
using System.Security.Cryptography;
using System.Web.Http.SelfHost;
using System.Web.Http;

namespace MUR_Blockchain_2._0
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private static readonly HttpClient httpClient = new HttpClient();
        public MainWindow()
        {
            InitializeComponent();
            GenerateKeys();
          

        }
       
        string lastMessage = "";
        static readonly object _lock = new object();
        static readonly Dictionary<int, TcpClient> list_clients = new Dictionary<int, TcpClient>();
        NetworkStream[] netstream = new NetworkStream[3];
        int port;

      
       // private  RSAParameters privateKey;
       
        private void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            buttonConnect.IsEnabled = false;
            APIbutton.IsEnabled = true;

            /*Broadcast on 255.255.255.255 that a server was started on textBoxPORT
             *Also start 3 clients that listen to broadcasts about new servers
            */





            IPAddress local_Address;
            local_Address = IPAddress.Parse("127.0.0.1");

           

            if (checkPorts())
            {
                
                //Start Api
                Thread api = new Thread(StartAPI);

                try
                {
                    api.IsBackground = true;
                    api.Start();

                }
                catch
                {
                    api.Join();
                    MessageBox.Show("Error occured");
                }


                Thread assigner = new Thread(client_assigner);
                    try
                    {
                        assigner.IsBackground = true;
                        assigner.Start();
                    }
                    catch
                    {
                        assigner.Join();
                        MessageBox.Show("Assigner Broke and crashed ¯\\_(ツ)_/¯ ");
                    }

                    // start server
                    Thread t = new Thread(listening_tcp);

                    try
                    {
                        t.IsBackground = true;
                        t.Start();

                    }
                    catch
                    {
                        t.Join();
                        MessageBox.Show("Error occured");
                    }
                    //////////////////////////////////////////


                    broadcastPort();
              
            }

            RTs223();
        }


        private async void buttonSend_Click(object sender, RoutedEventArgs e)
        {


            //Random rng = new Random();
            //Block newBlock = new Block(blockchain.chain.Count, "WHATS UP " + rng.Next(1000) + "\n", blockchain.getLastBlock().hash);
            var response = await httpClient.GetAsync("https://localhost:44366/api/block?command=new_block");

            var responseString = await response.Content.ReadAsStringAsync();

            responseString = responseString.Substring(1);
            responseString = responseString.Substring(0, responseString.Length - 1);
            responseString = responseString.Replace("\\", "");

           // MessageBox.Show(responseString);
            Block newBlock = JsonConvert.DeserializeObject<Block>(responseString);




            if (GlobalClass.blockchain.chain.Count > 0)
            {
                // blockchain.addBlock(newBlock, Int32.Parse(Difficulty.Text));
                GlobalClass.blockchain.chain.Add(newBlock);
                if (!GlobalClass.blockchain.validateChain())
                {

                    sync();
                }
            }
            else
            {
                sync();
            }
            string json = JsonConvert.SerializeObject(newBlock);

            textBoxContent.Text = GlobalClass.blockchain.ToString();
            textBoxContent.ScrollToEnd();
            broadcast(json);
        }

        public void broadcastServer(string data)
        {
            //sending the message to everybody by a server
           


            byte[] buffer = Encoding.UTF8.GetBytes(data /*+ Environment.NewLine*/);

            lock (_lock)
            {
                foreach (TcpClient c in list_clients.Values)
                {
                    NetworkStream stream = c.GetStream();

                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }
   

        public void listening_tcp()
        {
            try
            {
                IPAddress Local_Address = IPAddress.Parse("127.0.0.1");
                TcpListener listener = new TcpListener(Local_Address, port);
                Console.WriteLine("Server starting...");


                int count = 0;

                Console.WriteLine("Listening...");
                listener.Start();

                //Searching for new user


                while (count < 3)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    lock (_lock) list_clients.Add(count, client);
                    Console.WriteLine("New user is connecting...");

                    //starting new thread

                    Thread t = new Thread(handle_clients);
                    try
                    {
                          t.IsBackground = true;
                        t.Start(count);
                       
                        count++;
                    }catch
                    { t.Join(); }
                }


                
               
            }
            catch
            {
                MessageBox.Show("Error occured");
            }

        }

        public void client_assigner()
        {
         
            int listenPort = port;
            int clientsAssigned = 0;
            try
            {
                using (UdpClient listener = new UdpClient(listenPort))
                {
                    IPEndPoint listenEndPoint = new IPEndPoint(IPAddress.Any, listenPort);
                    while (true)
                    {
                        byte[] receivedData = listener.Receive(ref listenEndPoint);
                        
                        Console.Write("port recieved: ");
                        string serverPortString = Encoding.ASCII.GetString(receivedData);
                        Console.WriteLine(serverPortString); 
                        if (clientsAssigned < 3)
                        {
                            int serverPort = int.Parse(serverPortString);

                            assignClient(serverPort, clientsAssigned);
                           
                            clientsAssigned++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

        public void broadcastPort()
        {
            // ↓ THIS IS SENDING PORT I THINK ↓

            byte[] data = Encoding.ASCII.GetBytes(port.ToString());
            string ipAddress = "255.255.255.255";
            int sendPort = 8000;
            try
            {
                while (sendPort <= 9000)
                {
                    if (sendPort == port)
                        sendPort++;
                    using (var client = new UdpClient())
                    {
                        IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ipAddress), sendPort);
                        client.Connect(ep);
                        client.Send(data, data.Length);
                    }
                    sendPort++;
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            // ↑ THIS IS SENDING PORT I THINK ↑
        }

        public bool checkPorts()
        {
          
            var startingAtPort = 8000;
            var maxNumberOfPortsToCheck = 1000;
            var range = Enumerable.Range(startingAtPort, maxNumberOfPortsToCheck);
            var portsInUse =
                from p in range
                join used in System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners()
            on p equals used.Port
                select p;

            var FirstFreeUDPPortInRange = range.Except(portsInUse).FirstOrDefault();

            if (FirstFreeUDPPortInRange > 0)
            {
              
                port = FirstFreeUDPPortInRange;
            }
            else
            {
                MessageBox.Show("No Free ports found, please turn free some ports");
                // complain about lack of free ports
                return false;
            }

            return true;
        }

        private void RTs223()
        {
            if (textBoxUsername.Text == "Jew" || textBoxUsername.Text == "jew")
            {
                spendButton.IsEnabled = false;
            }
        }
        public void handle_clients(object o)
        {
            //initializing httpClient
            int id = (int)o;
            TcpClient client;
            lock (_lock) client = list_clients[id];

            try
            {


                //grabbing data from user

                while (true)
                {
                    NetworkStream stream = client.GetStream();
                    byte[] buffer = new byte[1024];
                    int byte_count = stream.Read(buffer, 0, buffer.Length);



                    if (byte_count == 0)
                    {
                        break;
                    }


                    //got data
                   
                    string data = Encoding.ASCII.GetString(buffer, 0, byte_count);
                   
                    if (data != "")
                    {
                       
                            //RECIEVED BLOCKCHAIN SHIT
                            Dispatcher.Invoke(new Action(() =>
                            {

                                if (data != lastMessage)
                                {
                                    broadcast(data);

                                    GlobalClass.blockchain.chain.Add(JsonConvert.DeserializeObject<Block>(data));
                                    if(!GlobalClass.blockchain.validateChain())
                                    {
                                        sync();
                                    }
                                    textBoxContent.Text = GlobalClass.blockchain.ToString();
                                    textBoxContent.ScrollToEnd();
                                    lastMessage = data;
                                }

                            }), DispatcherPriority.ContextIdle);

                       
                     }
                }

                //user disconnected

                lock (_lock) list_clients.Remove(id);
                client.Client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch
            {
               
            }
        }

        public void assignClient(int serverPort, int id)
        {
            TcpClient client = new TcpClient();
            client.Connect(IPAddress.Parse("127.0.0.1"), serverPort);

            int count = list_clients.Count;

            netstream[id] = client.GetStream();

     

            Thread t = new Thread(o => connection((TcpClient)o));
            try
            {
                t.IsBackground = true;
                t.Start(client);
            }
            catch
            {
                t.Join();
            }

        }

        public void clientSend(int id, string data)
        {
            try
            {
                
               
                while (!string.IsNullOrEmpty((data)))
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(data);
                    netstream[id].Write(buffer, 0, buffer.Length);
                   
                   data = "";
                }
               
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void connection(TcpClient client)
        {
            try
            {
                NetworkStream ns = client.GetStream();
                
                byte[] receivedBytes = new byte[1024];
                int byte_count;

                while ((byte_count = ns.Read(receivedBytes, 0, receivedBytes.Length)) > 0)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        string data = Encoding.ASCII.GetString(receivedBytes, 0, byte_count);
                        if (data != lastMessage)
                        {

                            GlobalClass.blockchain.chain.Add(JsonConvert.DeserializeObject<Block>(data));

                            if (!GlobalClass.blockchain.validateChain())
                            {
                                sync();
                            }

                            textBoxContent.Text = GlobalClass.blockchain.ToString();
                            textBoxContent.ScrollToEnd();
                            broadcast(data);
                            lastMessage = data;
                        }
                    }), DispatcherPriority.ContextIdle);

                }
            }
            catch
            {

            }
        }

        private void close_threads(object sender, System.ComponentModel.CancelEventArgs e)
        {
           if(!buttonConnect.IsEnabled)
                httpClient.GetAsync("https://localhost:44366/api/default?command=quit");
            Environment.Exit(Environment.ExitCode);
        }


        public void broadcast(string data)
        {
            lastMessage = data;
            try
            {
                broadcastServer(data);
            }
            catch
            {

            }
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    if (netstream[i] != null)
                        clientSend(i, data);
                }
            }
            catch (Exception ex)
            { MessageBox.Show(ex.ToString()); }
        }


        public async void sync()
        {

            var response = await httpClient.GetAsync("https://localhost:44366/api/default?command=check");

            var responseMessage = await response.Content.ReadAsStringAsync();

            responseMessage = cleanUpResponse(responseMessage);

            //MessageBox.Show("SYNC :\n " + responseMessage);

            if(responseMessage == "no")
            {
                response = await httpClient.GetAsync("https://localhost:44366/api/default?command=init");


                responseMessage = await response.Content.ReadAsStringAsync();

                responseMessage = cleanUpResponse(responseMessage);

                //  MessageBox.Show(responseMessage);
                GlobalClass.blockchain.chain.Clear();
                GlobalClass.blockchain = JsonConvert.DeserializeObject<Blockchain>(responseMessage);

                textBoxContent.Text = GlobalClass.blockchain.ToString();
                textBoxContent.ScrollToEnd();
                return;
            }
            //////////////////////////////////////////////////
           
            response = await httpClient.GetAsync("https://localhost:44366/api/block?command=last_block");

            responseMessage = await response.Content.ReadAsStringAsync();

            responseMessage = cleanUpResponse(responseMessage);
            //MessageBox.Show("here");
          //  MessageBox.Show("From Server:\n"+responseMessage + "\n\nFrom Client:\n" + cleanUpResponse(JsonConvert.SerializeObject(blockchain.getLastBlock())));

            Block block = JsonConvert.DeserializeObject<Block>(responseMessage);

            if (responseMessage == "{" + cleanUpResponse(JsonConvert.SerializeObject(GlobalClass.blockchain.getLastBlock())) + "}")
            {
                if (GlobalClass.blockchain.validateChain())
                {
                    MessageBox.Show("Already in Sync");
                    return;
                }
            }
            {

                response = await httpClient.GetAsync("https://localhost:44366/api/block?command=whole_blockchain");

                responseMessage = await response.Content.ReadAsStringAsync();

                responseMessage = cleanUpResponse(responseMessage);

                // MessageBox.Show("The Entire Blockchain From Server:\n" + responseMessage);
                GlobalClass.blockchain.chain.Clear();
                GlobalClass.blockchain = JsonConvert.DeserializeObject<Blockchain>(responseMessage);

            }


            textBoxContent.Text = GlobalClass.blockchain.ToString();
            textBoxContent.ScrollToEnd();
        }

        private string cleanUpResponse(string response)
        {
            response = response.Substring(1);
            response = response.Substring(0, response.Length - 1);
            response = response.Replace("\\", "");

            return response;
        }

        private void Button_Sync(object sender, RoutedEventArgs e)
        {
            sync();
        }

        private async void Button_Change_Diff(object sender, RoutedEventArgs e)
        {

            var response = await httpClient.GetAsync("https://localhost:44366/api/block?command=change_diff&num=" + Difficulty.Text);

            var responseMessage = await response.Content.ReadAsStringAsync();

            responseMessage = cleanUpResponse(responseMessage);

            MessageBox.Show(responseMessage);

        }

        public async void Connect()
        {

            
           
            string username = textBoxUsername.Text;
            textBoxUsername.IsEnabled = false;
          
            username = username.Replace(" ", "+");
             var response = await httpClient.GetAsync("https://localhost:44366/api/default?command=connect&username=" + username + "&key=" + Uri.EscapeDataString(GlobalClass.xmlPublicKey));
           // var response = await httpClient.GetAsync("https://localhost:44366/api/default?command=connect&username=" + username + "&port=" + port + "&key=" + Uri.EscapeDataString(xmlPublicKey));

            //response.Wait();



            var responseMessage =  await response.Content.ReadAsStringAsync();
            //var responseMessage = response.Result.Content.ReadAsStringAsync().Result;

            //MessageBox.Show(responseMessage);

            string clean = cleanUpResponse(responseMessage);

           

            string[] gotem = JsonConvert.DeserializeObject<string[]>(clean);
            GlobalClass.id = gotem[0];

            if (GlobalClass.id == "No")
            {
                MessageBox.Show("Something went wrong!");
                textBoxUsername.IsEnabled = true;
                buttonConnect.IsEnabled = true;
               
                return;

            }

            
           
            using (var rsa = new RSACryptoServiceProvider(4096))
            {

                rsa.PersistKeyInCsp = false; 
                rsa.FromXmlString(gotem[1]);

                GlobalClass.publicKey = rsa.ExportParameters(false);
               

               
            }

        
        }

        private async void Button_Connected_Users(object sender, RoutedEventArgs e)
        {
            var response = await httpClient.GetAsync("https://localhost:44366/api/default?command=get_client_num");

            var responseMessage = await response.Content.ReadAsStringAsync();



            MessageBox.Show("Number of Connected users :"+responseMessage);
        }

        private async void Button_Get_Log(object sender, RoutedEventArgs e)
        {
            var response = await httpClient.GetAsync("https://localhost:44366/api/default?command=get_log");

            var responseMessage = await response.Content.ReadAsStringAsync();
           
            //MessageBox.Show(cleanUpResponse(responseMessage));
            List<string> Log = JsonConvert.DeserializeObject<List<string>>(cleanUpResponse(responseMessage));


            string anotherLog = "";

            foreach(string log in Log)
            {
                anotherLog += log + "\n";
            }

            MessageBox.Show(anotherLog);

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {

            int number = Math.Abs(int.Parse(textGoldAmount.Text));

            byte[] encrypted = Encrypt(Encoding.UTF8.GetBytes(number.ToString()));
            
            var response = await httpClient.GetAsync("https://localhost:44366/api/default?command=spend&username=" + GlobalClass.id + "&number="+ BitConverter.ToString(encrypted));

            var responseMessage = await response.Content.ReadAsStringAsync();


           


            MessageBox.Show(responseMessage);
 

        }



        private void GenerateKeys()
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.PersistKeyInCsp = false;
                GlobalClass.privateKey = rsa.ExportParameters(true);
                GlobalClass.xmlPublicKey = rsa.ToXmlString(false);
            }
        }

        private byte[] Encrypt(byte[] input)
        {
            byte[] encrypted;
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.PersistKeyInCsp = false;
                rsa.ImportParameters(GlobalClass.publicKey);
                encrypted = rsa.Encrypt(input, true);
            }

            return encrypted;
        }

        private byte[] Decrypt(byte[] input, RSAParameters key)
        {
            byte[] decrypted;
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.PersistKeyInCsp = false;
                rsa.ImportParameters(key);
                decrypted = rsa.Decrypt(input, true);
            }

            return decrypted;
        }

        private byte[] Sign(byte[] input)
        {
            byte[] signature;
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.PersistKeyInCsp = false;
                rsa.ImportParameters(GlobalClass.privateKey);
                signature = rsa.SignData(input, CryptoConfig.MapNameToOID("SHA512"));

            }

            return signature;
        }


        private void CheckNumber(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            string text = "";
            //MessageBox.Show("SAAAAAA");
            if (textBox != null)
            {

                int n;
                bool isNumeric = int.TryParse(textBox.Text, out n);
                if (!isNumeric)
                {
                    if (textBox.Text.Length > 1)
                    {

                        foreach (char simp in textBox.Text)
                        {
                            isNumeric = int.TryParse(simp.ToString(), out n);
                            if (isNumeric)
                                text += simp;
                        }


                        textBox.Text = text;
                    }
                    else
                        textBox.Text = "";


                }


            }

        }

        private async void Button_Check_Balance(object sender, RoutedEventArgs e)
        {
            var response = await httpClient.GetAsync("https://localhost:44366/api/default?command=check_gold&username=" + GlobalClass.id);

            var responseMessage = await response.Content.ReadAsStringAsync();


            MessageBox.Show(responseMessage);

        }

        private async void Button_Request_Gold(object sender, RoutedEventArgs e)
        {
            if (textGoldAmount.Text != "")
            {
                string otherUser = textBoxTransUser.Text;
                string gold = textGoldAmount.Text;

                string signature = Convert.ToBase64String(Sign(Encoding.UTF8.GetBytes(otherUser + gold)));

               
                string[] content = { otherUser, gold};

            
                string data = JsonConvert.SerializeObject(content);

                data = BitConverter.ToString(Encrypt(Encoding.UTF8.GetBytes(data)));


                var response = await httpClient.GetAsync("https://localhost:44366/api/default?command=req_gold&username=" + GlobalClass.id + "&data="  + data + "&signature=" + Uri.EscapeDataString(signature));
               
              
                var responseMessage = await response.Content.ReadAsStringAsync();


                MessageBox.Show(responseMessage);

            }
            else
                MessageBox.Show("Please Enter Amount of Gold");

        }

        private async void Button_Send_Gold(object sender, RoutedEventArgs e)
        {
            if (textGoldAmount.Text != "")
            {
                string otherUser = textBoxTransUser.Text;
                string gold = textGoldAmount.Text;
                
                string signature = Convert.ToBase64String(Sign(Encoding.UTF8.GetBytes(otherUser + gold)));

                string[] content = { otherUser, gold };


                string data = JsonConvert.SerializeObject(content);

                data = BitConverter.ToString(Encrypt(Encoding.UTF8.GetBytes(data)));

                var response = await httpClient.GetAsync("https://localhost:44366/api/default?command=send_gold&username=" + GlobalClass.id + "&data=" + data + "&signature=" + Uri.EscapeDataString(signature));

                var responseMessage = await response.Content.ReadAsStringAsync();


                MessageBox.Show(responseMessage);
            }
            else
                MessageBox.Show("Please Enter Amount of Gold");
        }

        private async void Button_Check_Trans(object sender, RoutedEventArgs e)
        {
            var response = await httpClient.GetAsync("https://localhost:44366/api/default?command=check_trans&username=" + GlobalClass.id);

            var responseMessage = await response.Content.ReadAsStringAsync();


            MessageBox.Show(responseMessage.Replace("\\n","\n").Replace("\"",""));
           
        }

        private async void Button_Accept_All(object sender, RoutedEventArgs e)
        {
            var response = await httpClient.GetAsync("https://localhost:44366/api/block?command=acc_all&username=" + GlobalClass.id);


            var responseMessage = await response.Content.ReadAsStringAsync();


            if (cleanUpResponse(responseMessage) == "No Transactions")
                MessageBox.Show(cleanUpResponse(responseMessage));
            else
            {
                sync();
                string json = JsonConvert.SerializeObject(GlobalClass.blockchain.getLastBlock());
                broadcast(json);
            }
        }

        private async void Button_Decline_All(object sender, RoutedEventArgs e)
        {
            var response = await httpClient.GetAsync("https://localhost:44366/api/block?command=dec_all&username=" + GlobalClass.id);


            var responseMessage = await response.Content.ReadAsStringAsync();

            if (cleanUpResponse(responseMessage) == "No Transactions")
                MessageBox.Show(cleanUpResponse(responseMessage));
            else
            {
                sync();
                string json = JsonConvert.SerializeObject(GlobalClass.blockchain.getLastBlock());
                broadcast(json);
            }
        }

        private async void Button_Accept(object sender, RoutedEventArgs e)
        {
            if (textTransID.Text != "")
            {
                string number = textTransID.Text;
                number = BitConverter.ToString(Encrypt(Encoding.UTF8.GetBytes(number)));
                var response = await httpClient.GetAsync("https://localhost:44366/api/block?command=acc&username=" + GlobalClass.id + "&number=" + number);
                var responseMessage = await response.Content.ReadAsStringAsync();
                //MessageBox.Show(responseMessage);

                if (cleanUpResponse(responseMessage) == "no")
                {
                    MessageBox.Show("Something went wrong, check ID");
                    return;
                }
                else if(cleanUpResponse(responseMessage) == "You")
                {
                    MessageBox.Show("You do not own enough Gold");
                    return;
                }
                else if (cleanUpResponse(responseMessage) == "They")
                {
                    MessageBox.Show("The user does not own enought Gold");
                    return;
                }

                Block newBlock = JsonConvert.DeserializeObject<Block>(cleanUpResponse(responseMessage));

                GlobalClass.blockchain.chain.Add(newBlock);

                if (!GlobalClass.blockchain.validateChain())
                {
                    sync();
                }


                string json = JsonConvert.SerializeObject(newBlock);

                textBoxContent.Text = GlobalClass.blockchain.ToString();
                textBoxContent.ScrollToEnd();
                broadcast(json);
            }
            else
                MessageBox.Show("Please Enter Transaction ID");

        }

        private async void Button_Decline(object sender, RoutedEventArgs e)
        {
            if (textTransID.Text != "")
            {

                string number = textTransID.Text;
                number = BitConverter.ToString(Encrypt(Encoding.UTF8.GetBytes(number)));
                var response = await httpClient.GetAsync("https://localhost:44366/api/block?command=dec&username=" + GlobalClass.id + "&number=" + number);
                var responseMessage = await response.Content.ReadAsStringAsync();
                //MessageBox.Show(responseMessage);

                if (cleanUpResponse(responseMessage) == "no")
                {
                    MessageBox.Show("Something went wrong, check ID");
                    return;
                }

                Block newBlock = JsonConvert.DeserializeObject<Block>(cleanUpResponse(responseMessage));

                GlobalClass.blockchain.chain.Add(newBlock);

                if (!GlobalClass.blockchain.validateChain())
                {
                    sync();
                }


                string json = JsonConvert.SerializeObject(newBlock);

                textBoxContent.Text = GlobalClass.blockchain.ToString();
                textBoxContent.ScrollToEnd();
                broadcast(json);
            }
            else
                MessageBox.Show("Please Enter Transaction ID");

        }

        private void StartAPI()
        {
            var config = new HttpSelfHostConfiguration("http://localhost:" + (port+1000));

            config.Routes.MapHttpRoute("API Default", "api/{controller}");

            using (HttpSelfHostServer server = new HttpSelfHostServer(config))
            {
                server.OpenAsync().Wait();
                // MessageBox.Show("API RUNNING");
               //This is a stupid solution but it works
                while(true)
                {
                    Thread.Sleep(10000);
                }

            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Connect();
            sync();
            APIbutton.IsEnabled = false;
            spendButton.IsEnabled = true;
            requestButton.IsEnabled = true;
            checkBalanceButton.IsEnabled = true;
            checkRequestsButton.IsEnabled = true;
            checkMyRequestsButton.IsEnabled = true;
            accAllButton.IsEnabled = true;
            accButton.IsEnabled = true;
            decAllButton.IsEnabled = true;
            decButton.IsEnabled = true;
        }

        private async void Button_Check_MyTrans(object sender, RoutedEventArgs e)
        {
            var response = await httpClient.GetAsync("https://localhost:44366/api/default?command=check_mytrans&username=" + GlobalClass.id);

            var responseMessage = await response.Content.ReadAsStringAsync();


            MessageBox.Show(responseMessage.Replace("\\n", "\n").Replace("\"", ""));

        }
    }
}
