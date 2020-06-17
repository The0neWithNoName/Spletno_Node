using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Windows;

namespace MUR_Blockchain_2._0
{
    public class DefaultController : ApiController
    {

        public DefaultController()
        {

        }


        private readonly HttpClient httpClient = new HttpClient();

        public string Get()
        {
            return "Welcome To a Node API";
        }

        public string Get(string command)
        {
            if (command == "check_wallet")
            {
                var response = httpClient.GetAsync("https://localhost:44366/api/default?command=check_gold&username=" + GlobalClass.id);

                response.Wait();

                //var responseMessage = await response.Content.ReadAsStringAsync();
                var responseMessage = response.Result.Content.ReadAsStringAsync().Result;

                return cleanUpResponse(responseMessage);
            }
            else if (command == "check_trans")
            {
                var response = httpClient.GetAsync("https://localhost:44366/api/default?command=check_trans&username=" + GlobalClass.id);


                response.Wait();
                var responseMessage = response.Result.Content.ReadAsStringAsync().Result;

                return responseMessage.Replace("\\n", "\n").Replace("\"", "");
            }
            else if (command == "check_my_trans")
            {
                var response = httpClient.GetAsync("https://localhost:44366/api/default?command=check_mytrans&username=" + GlobalClass.id);


                response.Wait();
                var responseMessage = response.Result.Content.ReadAsStringAsync().Result;

                return responseMessage.Replace("\\n", "\n").Replace("\"", "");
            }
            else if (command == "accept_all")
            {
                var response = httpClient.GetAsync("https://localhost:44366/api/block?command=acc_all&username=" + GlobalClass.id);



                response.Wait();
                var responseMessage = response.Result.Content.ReadAsStringAsync().Result;


                if (cleanUpResponse(responseMessage) == "No Transactions")
                    return(cleanUpResponse(responseMessage));
                else
                {
                    sync();
                    string json = JsonConvert.SerializeObject(GlobalClass.blockchain.getLastBlock());
                    Application.Current.Dispatcher.Invoke(() =>
                    {

                        ((MainWindow)System.Windows.Application.Current.MainWindow).textBoxContent.Text = GlobalClass.blockchain.ToString();
                        ((MainWindow)System.Windows.Application.Current.MainWindow).textBoxContent.ScrollToEnd();

                        ((MainWindow)System.Windows.Application.Current.MainWindow).broadcast(json);
                    });
                    return "accepted";
                }
            }
            else if (command == "decline_all")
            {
                var response = httpClient.GetAsync("https://localhost:44366/api/block?command=dec_all&username=" + GlobalClass.id);


                response.Wait();
                var responseMessage = response.Result.Content.ReadAsStringAsync().Result;

                if (cleanUpResponse(responseMessage) == "No Transactions")
                  return (cleanUpResponse(responseMessage));
                else
                {
                    sync();
                    string json = JsonConvert.SerializeObject(GlobalClass.blockchain.getLastBlock());
                    Application.Current.Dispatcher.Invoke(() =>
                    {

                        ((MainWindow)System.Windows.Application.Current.MainWindow).textBoxContent.Text = GlobalClass.blockchain.ToString();
                        ((MainWindow)System.Windows.Application.Current.MainWindow).textBoxContent.ScrollToEnd();

                        ((MainWindow)System.Windows.Application.Current.MainWindow).broadcast(json);
                    });
                }

                return "Declined";
            }


                
                return "error";
        }

        public  string Get(string command, string data)
        {
            if (command == "connect")
            {

                string username = data;

                var response = httpClient.GetAsync("https://localhost:44366/api/default?command=connect&username=" + username + "&key=" + Uri.EscapeDataString(GlobalClass.xmlPublicKey));
                // var response = await httpClient.GetAsync("https://localhost:44366/api/default?command=connect&username=" + username + "&port=" + port + "&key=" + Uri.EscapeDataString(xmlPublicKey));

                response.Wait();



                //var responseMessage = await response.Content.ReadAsStringAsync();
                var responseMessage = response.Result.Content.ReadAsStringAsync().Result;

                //MessageBox.Show(responseMessage);

                string clean = cleanUpResponse(responseMessage);



                string[] gotem = JsonConvert.DeserializeObject<string[]>(clean);
                GlobalClass.id = gotem[0];

                if (GlobalClass.id == "No")
                {
                    // MessageBox.Show("Something went wrong!");


                    return "error";

                }



                using (var rsa = new RSACryptoServiceProvider(4096))
                {

                    rsa.PersistKeyInCsp = false;
                    rsa.FromXmlString(gotem[1]);

                    GlobalClass.publicKey = rsa.ExportParameters(false);



                }


                sync();


                return "Connected";
            }
            else if (command == "accept_trans")
            {
                string number = data;
                number = BitConverter.ToString(Encrypt(Encoding.UTF8.GetBytes(number)));
                var response =  httpClient.GetAsync("https://localhost:44366/api/block?command=acc&username=" + GlobalClass.id + "&number=" + number);
                response.Wait();
                var responseMessage = response.Result.Content.ReadAsStringAsync().Result;


                if (cleanUpResponse(responseMessage) == "no")
                {
                    return ("Something went wrong, check ID");
                   
                }
                else if (cleanUpResponse(responseMessage) == "You")
                {
                    return ("You do not own enough Gold");
                  
                }
                else if (cleanUpResponse(responseMessage) == "They")
                {
                    return ("The user does not own enought Gold");
                   
                }

                Block newBlock = JsonConvert.DeserializeObject<Block>(cleanUpResponse(responseMessage));

                GlobalClass.blockchain.chain.Add(newBlock);

                if (!GlobalClass.blockchain.validateChain())
                {
                    sync();
                }


                string json = JsonConvert.SerializeObject(newBlock);

                Application.Current.Dispatcher.Invoke(() =>
                {

                    ((MainWindow)System.Windows.Application.Current.MainWindow).textBoxContent.Text = GlobalClass.blockchain.ToString();
                    ((MainWindow)System.Windows.Application.Current.MainWindow).textBoxContent.ScrollToEnd();

                    ((MainWindow)System.Windows.Application.Current.MainWindow).broadcast(json);
                });


                return "Transaction Accepted";
            }
            else if (command == "decline_trans")
            {

                string number = data;
                number = BitConverter.ToString(Encrypt(Encoding.UTF8.GetBytes(number)));
                var response =  httpClient.GetAsync("https://localhost:44366/api/block?command=dec&username=" + GlobalClass.id + "&number=" + number);
                response.Wait();
                var responseMessage = response.Result.Content.ReadAsStringAsync().Result;

                if (cleanUpResponse(responseMessage) == "no")
                {
                    return ("Something went wrong, check ID");
                    
                }

                Block newBlock = JsonConvert.DeserializeObject<Block>(cleanUpResponse(responseMessage));

                GlobalClass.blockchain.chain.Add(newBlock);

                if (!GlobalClass.blockchain.validateChain())
                {
                    sync();
                }


                string json = JsonConvert.SerializeObject(newBlock);


                Application.Current.Dispatcher.Invoke(() =>
                {

                    ((MainWindow)System.Windows.Application.Current.MainWindow).textBoxContent.Text = GlobalClass.blockchain.ToString();
                    ((MainWindow)System.Windows.Application.Current.MainWindow).textBoxContent.ScrollToEnd();

                    ((MainWindow)System.Windows.Application.Current.MainWindow).broadcast(json);
                });


                return "Transaction Declined";
            }
           
            return "error";
        }


        public string Get(string command, string username, string number)
        {
            if (command == "request_gold")
            {
                string otherUser = username;
                string gold = number;

                string signature = Convert.ToBase64String(Sign(Encoding.UTF8.GetBytes(otherUser + gold)));


                string[] content = { otherUser, gold };


                string data = JsonConvert.SerializeObject(content);

                data = BitConverter.ToString(Encrypt(Encoding.UTF8.GetBytes(data)));


                var response = httpClient.GetAsync("https://localhost:44366/api/default?command=req_gold&username=" + GlobalClass.id + "&data=" + data + "&signature=" + Uri.EscapeDataString(signature));

                response.Wait();
                var responseMessage = response.Result.Content.ReadAsStringAsync().Result;

                return cleanUpResponse(responseMessage);

            }
            else if (command == "send_gold")
            {
                string otherUser = username;
                string gold = number;

                string signature = Convert.ToBase64String(Sign(Encoding.UTF8.GetBytes(otherUser + gold)));

                string[] content = { otherUser, gold };


                string data = JsonConvert.SerializeObject(content);

                data = BitConverter.ToString(Encrypt(Encoding.UTF8.GetBytes(data)));

                var response =  httpClient.GetAsync("https://localhost:44366/api/default?command=send_gold&username=" + GlobalClass.id + "&data=" + data + "&signature=" + Uri.EscapeDataString(signature));
                
                response.Wait();
                var responseMessage = response.Result.Content.ReadAsStringAsync().Result;

                return cleanUpResponse(responseMessage);
            }
            return "error";
        }


        private string cleanUpResponse(string response)
        {
            response = response.Substring(1);
            response = response.Substring(0, response.Length - 1);
            response = response.Replace("\\", "");

            return response;
        }



        public async void sync()
        {

            var response = await httpClient.GetAsync("https://localhost:44366/api/default?command=check");

            var responseMessage = await response.Content.ReadAsStringAsync();

            responseMessage = cleanUpResponse(responseMessage);

            //MessageBox.Show("SYNC :\n " + responseMessage);

            if (responseMessage == "no")
            {
                response = await httpClient.GetAsync("https://localhost:44366/api/default?command=init");


                responseMessage = await response.Content.ReadAsStringAsync();

                responseMessage = cleanUpResponse(responseMessage);

                //  MessageBox.Show(responseMessage);
                GlobalClass.blockchain.chain.Clear();
                GlobalClass.blockchain = JsonConvert.DeserializeObject<Blockchain>(responseMessage);

                Application.Current.Dispatcher.Invoke(() =>
                {

                    ((MainWindow)System.Windows.Application.Current.MainWindow).textBoxContent.Text = GlobalClass.blockchain.ToString();
                    ((MainWindow)System.Windows.Application.Current.MainWindow).textBoxContent.ScrollToEnd();

                   
                });

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
                   // MessageBox.Show("Already in Sync");
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
            Application.Current.Dispatcher.Invoke(() =>
            {

                ((MainWindow)System.Windows.Application.Current.MainWindow).textBoxContent.Text = GlobalClass.blockchain.ToString();
                ((MainWindow)System.Windows.Application.Current.MainWindow).textBoxContent.ScrollToEnd();

               
            });

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

    }
}
