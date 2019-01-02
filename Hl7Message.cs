using System;
using System.IO;
using System.Text;

namespace HL7
{
    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="">InvaliDataEceeption</exception>
    public class Hl7Message
    {
        
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PatientId { get; set; }
        public DateTime BirthDate { get; set; }
        public string Gender { get; set; }
        /// <summary>
        /// Data from zri/zpd segment , this data is with
        /// the GE hl7 special encoding AND is UuEncoded
        /// </summary>
        public string OriginalZriSegment { get; set; }
        /// <summary>
        /// Original message without any changes 
        /// </summary>
        public string OriginalMessage { get; set; }



        /// <summary>
        /// Create new instance of Hl7Message 
        /// </summary>
        /// <param name="message">String representing the complete HL7 message</param>
        /// <exception cref="InvalidDataException"></exception>
        public Hl7Message(string message)
        {
            if (!IsHL7Message(message))
            {
                throw new InvalidDataException($"The data provided does not contain a valid hl7 message . ");
            }
            OriginalMessage = message;
        
            if (message.Substring(0, 3) == "MSH")
            {
                ParseMessage(message);
                OriginalZriSegment = GetOriginalZriSegment(message);
            }
            else
            {
                throw new Exception("File/Message Not An HL7 Object");
            }
        }

        /// <summary>
        ///  Create new instance of Hl7Message 
        /// </summary>
        /// <param name="file">FileInfo of the file with the HL7 Message</param>
        /// <exception cref="InvalidDataException"></exception>
        public Hl7Message(FileInfo file)
        {
          
            BinaryReader br = null;
            StreamReader sr = null;
            FileStream fs = null;
            try
            {
                fs = new FileStream(file.FullName, FileMode.Open);
                br = new BinaryReader(fs);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            byte[] msh = br.ReadBytes(3);
            
                sr = new StreamReader(fs);
                string message = sr.ReadToEnd();
                if (!IsHL7Message(message))
                {
                    throw new InvalidDataException($"The data provided does not contain a valid hl7 message . ");
                }
                OriginalMessage = message;
                ParseMessage(message);
                OriginalZriSegment = GetOriginalZriSegment(message);
                sr.Close();
                fs.Close();
                br.Close();

        }
              
        /// <summary>
        /// returns the original PDF binary data of the ZRI/ZPD segment
        /// </summary>
        /// <returns>PDF binary data byte[]</returns>
        public byte[] GetOriginalBinaryDataPDF()
        {
            string pdfHl7Decoded = GetHl7DecodedZriSegment();
            byte[] binaryData = GetUUDecodeData(pdfHl7Decoded);
            return binaryData;
        }
               
        /// <summary>
        /// Saves the embedded ECG record as PDF
        /// </summary>
        /// <param name="path">Destination to save the PDF</param>
        public void SavePDFFile(String path)
        {
            //UUDecoding here
            byte[] decodedDataBytes = this.GetOriginalBinaryDataPDF();

            FileStream pdfFileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
            using (pdfFileStream)
            {
                BinaryWriter br = new BinaryWriter(pdfFileStream);
                using (br)
                {
                    br.Write(decodedDataBytes);
                    br.Flush();
                }
            }

        }

       



        private void ParseMessage(string message)
        {
            GetPatientDemographics(message);
            GetOriginalZriSegment(message);

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
        private string GetOriginalZriSegment(string message)
        {
            return message.Substring(message.IndexOf("ZRI", StringComparison.Ordinal), message.Length - message.IndexOf("ZRI", StringComparison.Ordinal));
        }
        private string GetHl7DecodedZriSegment()
        {
            try
            {
                string[] zriComponents = this.OriginalZriSegment.Split('|');
                string[] pdfSubComponents = zriComponents[3].Split('^');
                int originalDataSize = int.Parse(pdfSubComponents[2]);
                string hl7AndUuencodedData = pdfSubComponents[4];
                StringBuilder hl7DecodedDataList = new StringBuilder();
                for (int i = 0; i <= originalDataSize; i++)
                {
                    if (i == originalDataSize)
                    {
                        break;
                    }

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
                        //logger.Debug($"line number {lineNumber}  i : {i} (found \r\n)");
                        //lineNumber++;
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
            catch (Exception ex)
            {
                throw (ex);
            }

        }
        private byte[] GetUUDecodeData(string data)
        {
            try
            {
                byte[] dataBytes = Encoding.ASCII.GetBytes(data);
                MemoryStream inputStream = new MemoryStream(dataBytes);
                MemoryStream outputStream = new MemoryStream();

                Codecs.UUDecode(inputStream, outputStream);
                byte[] decodedDataBytes = outputStream.ToArray();
                return decodedDataBytes;
            }
            catch (Exception ex)
            {
                throw (ex);
            }

        }
        private bool IsHL7Message(string message)
        {
            if (message.Contains("PID") && message.Contains("MSH") && message.Contains("PV1"))
            {
                return true;
            }
            return false;

        }
    }
    
}

