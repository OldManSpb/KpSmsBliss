/*
 * Copyright 2017 Mikhail Shiryaev & Andrew Saenko
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * 
 * Product  : Rapid SCADA
 * Module   : KpEmail
 * Summary  : Device communication logic
 * 
 * Author   : Mikhail Shiryaev & Andrew Saenko
 * Created  : 2016
 * Modified : 2017
 * 
 * Description
 * Sending email notifications.
 */

using Scada.Comm.Devices.AB;
using Scada.Comm.Devices.KpSmsGate;
using Scada.Data.Models;
using Scada.Data.Tables;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Collections.Specialized;

namespace Scada.Comm.Devices
{
    /// <summary>
    /// Device communication logic
    /// <para>Логика работы КП</para>
    /// </summary>
    public class KpSmsBlissLogic : KPLogic
    {
        private AB.AddressBook addressBook; // адресная книга, общая для линии связи
        private Config config;              // конфигурация соединения с почтовым сервером
        private bool fatalError;            // фатальная ошибка при инициализации КП
        private string state;               // состояние КП
        private bool writeState;            // вывести состояние КП


        /// <summary>
        /// Конструктор
        /// </summary>
        public KpSmsBlissLogic(int number)
            : base(number)
        {
            CanSendCmd = true;
            ConnRequired = false;
            WorkState = WorkStates.Normal;

            addressBook = null;
            config = new Config();
            
            fatalError = false;
            state = "";
            writeState = false;

            InitKPTags(new List<KPTag>()
            {
                new KPTag(1, Localization.UseRussian ? "Отправлено СМС" : "Sent SMS")
            });
        }


        /// <summary>
        /// Загрузить конфигурацию соединения с сервером
        /// </summary>
        private void LoadConfig()
        {
            string errMsg;
            fatalError = !config.Load(Config.GetFileName(AppDirs.ConfigDir, Number), out errMsg);

            if (fatalError)
            {
                state = Localization.UseRussian ? 
                    "Отправка СМС уведомлений невозможна" : 
                    "Sending notifocations is impossible";
                throw new Exception(errMsg);
            }
            else
            {
                state = Localization.UseRussian ? 
                    "Ожидание команд..." :
                    "Waiting for commands...";
            }
        }

        

        /// <summary>
        /// Попытаться получить почтовое сообщение из команды ТУ
        /// </summary>
        private bool TryGetMessage(Command cmd, out SmsMessage message)
        {

        
            string cmdDataStr = cmd.GetCmdDataStr();
            int ind1 = cmdDataStr.IndexOf(';');
            

            if (ind1 >= 0)
            {
                string recipient = cmdDataStr.Substring(0, ind1);
                
                string text = cmdDataStr.Substring(ind1 + 1);

                List<string> phone_numbers = new List<string>();

                if (addressBook == null)
                {
                    
                    // добавление телефона получателя из данных команды
                    phone_numbers.Add(recipient);
                }
                else
                {
                    
                    // поиск телефонов получателей в адресной книге
                    AB.AddressBook.ContactGroup contactGroup = addressBook.FindContactGroup(recipient);
                    if (contactGroup == null)
                    {
                        AB.AddressBook.Contact contact = addressBook.FindContact(recipient);
                        if (contact == null)
                        {
                            // добавление телефона получателя из данных команды
                            phone_numbers.Add(recipient);
                        }
                        else
                        {
                            // добавление телефона получателя из контакта
                            phone_numbers.AddRange(contact.PhoneNumbers);
                        }
                    }
                    else
                    {
                        // добавление телефонов получателей из группы контактов
                        foreach (AB.AddressBook.Contact contact in contactGroup.Contacts)
                            phone_numbers.AddRange(contact.PhoneNumbers);
                    }
                }

                // создание сообщения
                message = CreateMessage(phone_numbers, text);
                return message != null;
            }
            else
            {
                message = null;
                return false;
            }
        }

        /// <summary>
        /// Создать SMS сообщение
        /// </summary>
        private SmsMessage CreateMessage(List<string> phone_nums, string text)
        {
            SmsMessage message = new SmsMessage();

            foreach (string phone in phone_nums)
            {
                message.AddPhoneNo(phone);

            }

            if (phone_nums.Count > 0)
            {
                message.message = text;

                return message;                

            }
            else
            {
                WriteToLog(string.Format(Localization.UseRussian ?
                    "Некорректные номера телефонов получателя" :
                    "Incorrect receivers addresses "));
                return null;
            }

           
        }

        /// <summary>
        /// Отправить SMS сообщение
        /// </summary>
        private bool SendMessage(SmsMessage message)
        {

            foreach (string phone in message.phone_numbers)
            {

                using (var webClient = new WebClient())
                {
                    NameValueCollection parameters = new NameValueCollection();

                    parameters.Add("login", config.User);

                    parameters.Add("password", config.Password);

                    parameters.Add("phone", phone);

                    parameters.Add("text", message.message);

                    parameters.Add("sender", config.UserDisplayName);

                    webClient.QueryString.Add(parameters);

                    

                    try
                    {
                        var response = webClient.DownloadString(config.Host);

                        if(Localization.UseRussian)
                            WriteToLog("Сообщение '" + message.message + "' от '" + config.UserDisplayName + "' отправлено на номер " + phone);
                        else
                            WriteToLog("Message '" + message.message + "' from '" + config.UserDisplayName + "' is sent to " + phone);

                        if(Localization.UseRussian)
                            WriteToLog("Ответ сервера: " + response);
                        else
                            WriteToLog("Server responce: " + response);

                    }
                    catch (WebException ex)
                    {
                        WriteToLog(ex.Message);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Преобразовать данные тега КП в строку
        /// </summary>
        protected override string ConvertTagDataToStr(int signal, SrezTableLight.CnlData tagData)
        {
            if (tagData.Stat > 0)
            {
                if (signal == 1)
                    return tagData.Val.ToString("N0");
            }

            return base.ConvertTagDataToStr(signal, tagData);
        }


        /// <summary>
        /// Выполнить сеанс опроса КП
        /// </summary>
        public override void Session()
        {
            if (writeState)
            {
                WriteToLog("");
                WriteToLog(state);
                writeState = false;
            }

            Thread.Sleep(ScadaUtils.ThreadDelay);
        }

        /// <summary>
        /// Отправить команду ТУ
        /// </summary>
        public override void SendCmd(Command cmd)
        {
            base.SendCmd(cmd);
            lastCommSucc = false;

            if (fatalError)
            {
                WriteToLog(state);
            }
            else
            {
                if (cmd.CmdNum == 1 && cmd.CmdTypeID == BaseValues.CmdTypes.Binary)
                {
                    SmsMessage message = new SmsMessage();

                    if (TryGetMessage(cmd, out message))
                    {
                        if (SendMessage(message))
                            lastCommSucc = true;

                        // задержка позволяет ограничить скорость отправки писем
                        Thread.Sleep(ReqParams.Delay);
                    }
                    else
                    {
                        WriteToLog(CommPhrases.IncorrectCmdData);
                    }
                }
                else
                {
                    WriteToLog(CommPhrases.IllegalCommand);
                }

                writeState = true;
            }

            CalcCmdStats();
        }

        /// <summary>
        /// Выполнить действия при запуске линии связи
        /// </summary>
        public override void OnCommLineStart()
        {
            writeState = true;
            addressBook = AbUtils.GetAddressBook(AppDirs.ConfigDir, CommonProps, WriteToLog);
            LoadConfig();
            SetCurData(0, 0, 1);
        }

        private class SmsMessage
        {
            public SmsMessage()
            {
               
                phone_numbers = new List<string>();
            }
            public bool AddPhoneNo (string phone)
            {
                if (phone.StartsWith("+") && phone.Length > 10)
                {
                    phone_numbers.Add(phone);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            public string message;
            public List<string> phone_numbers;
        }

    }
}
