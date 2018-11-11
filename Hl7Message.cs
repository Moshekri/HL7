using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using HL7;

namespace HL7
{
    public class Hl7Message
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PatientId { get; set; }
        public DateTime BirthDate { get; set; }
        public string Gender { get; set; }
        public string ZriSegment { get; set; }
        public string OriginalMessage { get; set; }

        public Hl7Message(string message)
        {
            if (message.Substring(0, 3) == "MSH")
            {
                ParseMessage(message);
            }
            else
            {
                throw new Exception("File/Message Not An HL7 Object");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <exception cref="system.argument"></exception>
        
        public Hl7Message(FileInfo file)
        {
            BinaryReader br;
            StreamReader sr;
            FileStream fs;
            try
            {
                 fs = new FileStream(file.FullName, FileMode.Open);
                 br = new BinaryReader(fs);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            byte[] msh = br.ReadBytes(3);
            if (Encoding.UTF8.GetString(msh) == "MSH")
            {
                sr = new StreamReader(fs);
                string message = sr.ReadToEnd();
                ParseMessage(message);
                sr.Close();
                fs.Close();
                br.Close();

            }
            else
            {
                fs.Close();
                br.Close();
                throw new Exception("File/Message Not An HL7 Object");
            }

        }
        private void ParseMessage(string message)
        {
            GetPatientDemographics(message);
            SetZriSegment(message);

        }
        private void GetPatientDemographics(string message)
        {


            string pid = message.Substring(message.IndexOf("PID", StringComparison.Ordinal), message.Length - message.IndexOf("PID", StringComparison.Ordinal));
            string[] pidComponents = pid.Split(new char[] { '|' }, StringSplitOptions.None);
            this.PatientId = pidComponents[2];
            this.Gender = pidComponents[8];
            if (pidComponents[7] != string.Empty)
            {
                DateTime dob;
                if (DateTime.TryParse(pidComponents[7], out dob))
                {
                    this.BirthDate = dob;
                }
            }
            else
            {
                this.BirthDate = new DateTime(1800, 1, 1);
            }
            this.LastName = pidComponents[5].Split('^')[0];
            this.FirstName = pidComponents[5].Split('^')[1];


        }
        private void SetZriSegment(string message)
        {
            this.ZriSegment = message.Substring(message.IndexOf("ZRI", StringComparison.Ordinal), message.Length - message.IndexOf("ZRI", StringComparison.Ordinal));
        }
        /// <summary>
        /// This Method will return HL7 Decoded ZRI Segment 
        /// </summary>
        /// <returns>String</returns>
        internal string GetHl7DecodedZriSegment()
        {

            string[] zriComponents = this.ZriSegment.Split('|');
            string[] pdfSubComponents = zriComponents[3].Split('^');
            int originalDataSize = int.Parse(pdfSubComponents[2]);
            string hl7AndUuencodedData = pdfSubComponents[4];

            StringBuilder hl7DecodedDataList = new StringBuilder();

            for (int i = 0; i <= originalDataSize; i++)
            {
                if (hl7AndUuencodedData[i] == '\\' && hl7AndUuencodedData[i + 1] == 'F' && hl7AndUuencodedData[i + 2] == '\\')
                {
                    hl7DecodedDataList.Append('|');
                    i += 2;
                }
                else if (hl7AndUuencodedData[i] == '\\' && hl7AndUuencodedData[i + 1] == 'S' && hl7AndUuencodedData[i + 2] == '\\')
                {
                    hl7DecodedDataList.Append('^');
                    i += 2;
                }
                else if (hl7AndUuencodedData[i] == '\\' && hl7AndUuencodedData[i + 1] == 'T' && hl7AndUuencodedData[i + 2] == '\\')
                {
                    hl7DecodedDataList.Append('&');
                    i += 2;
                }
                else if (hl7AndUuencodedData[i] == '\\' && hl7AndUuencodedData[i + 1] == 'R' && hl7AndUuencodedData[i + 2] == '\\')
                {
                    hl7DecodedDataList.Append('~');
                    i += 2;
                }
                else if (hl7AndUuencodedData[i] == '\\' && hl7AndUuencodedData[i + 1] == 'E' && hl7AndUuencodedData[i + 2] == '\\')
                {
                    hl7DecodedDataList.Append('\\');
                    i += 2;
                }
                else if (hl7AndUuencodedData[i] == '\\' && hl7AndUuencodedData[i + 1] == 'X' && hl7AndUuencodedData[i + 2] == '0' && hl7AndUuencodedData[i + 3] == 'D' && hl7AndUuencodedData[i + 4] == '\\' && hl7AndUuencodedData[i] == '\\' && hl7AndUuencodedData[i + 1] == 'X' && hl7AndUuencodedData[i + 2] == '0' && hl7AndUuencodedData[i + 3] == 'A' && hl7AndUuencodedData[i + 4] == '\\')
                {
                    hl7DecodedDataList.Append('\r');
                    hl7DecodedDataList.Append('\n');
                    i += 8;
                }
                else if (hl7AndUuencodedData[i] == '\\' && hl7AndUuencodedData[i + 1] == 'X' && hl7AndUuencodedData[i + 2] == '0' && hl7AndUuencodedData[i + 3] == 'D' && hl7AndUuencodedData[i + 4] == '\\')
                {
                    hl7DecodedDataList.Append('\r');
                    i += 4;
                }
                else if (hl7AndUuencodedData[i] == '\\' && hl7AndUuencodedData[i + 1] == 'X' && hl7AndUuencodedData[i + 2] == '0' && hl7AndUuencodedData[i + 3] == 'A' && hl7AndUuencodedData[i + 4] == '\\')
                {
                    hl7DecodedDataList.Append('\n');
                    i += 4;
                }
                else
                {
                    hl7DecodedDataList.Append(hl7AndUuencodedData[i]);
                }
            }
            return hl7DecodedDataList.ToString();
        }

        
    }
}

