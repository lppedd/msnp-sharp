#region Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice.
All rights reserved. http://code.google.com/p/msnp-sharp/

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its
  contributors may be used to endorse or promote products derived from this
  software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 'AS IS'
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
THE POSSIBILITY OF SUCH DAMAGE. 
*/
#endregion

using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Globalization;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    /// <summary>
    /// Represents a single MSNSLPMessage.
    /// Usually this message is contained in a P2P Message.
    /// </summary>
    [Serializable()]
    public class MSNSLPMessage : NetworkMessage
    {
        private string startLine;
        private Encoding encoding = Encoding.UTF8;
        private MimeDictionary mimeHeaders = new MimeDictionary();
        private MimeDictionary mimeBodies = new MimeDictionary();

        public MSNSLPMessage()
        {
            Via = "MSNSLP/1.0/TLP";
            Branch = Guid.NewGuid().ToString("B").ToUpperInvariant();
            CSeq = 0;
            CallId = Guid.NewGuid();
            MaxForwards = 0;
            ContentType = "text/unknown";
            mimeHeaders["Content-Length"] = "0";
        }


        public string StartLine
        {
            get
            {
                return startLine;
            }
            set
            {
                startLine = value;
            }
        }



        /// <summary>
        /// Defaults to UTF8
        /// </summary>
        public Encoding Encoding
        {
            get
            {
                return encoding;
            }
            set
            {
                encoding = value;
            }
        }

        public int MaxForwards
        {
            get
            {
                return int.Parse(mimeHeaders["Max-Forwards"], System.Globalization.CultureInfo.InvariantCulture);
            }
            set
            {
                mimeHeaders["Max-Forwards"] = value.ToString();
            }
        }

        public string To
        {
            get
            {
                return mimeHeaders["To"];
            }
            set
            {
                mimeHeaders["To"] = value;
            }
        }

        public string From
        {
            get
            {
                return mimeHeaders["From"];
            }
            set
            {
                mimeHeaders["From"] = value;
            }
        }

        /// <summary>
        /// The contact that send the message.
        /// </summary>
        public string FromMail
        {
            get
            {
                return From.Replace("<msnmsgr:", String.Empty).Replace(">", String.Empty);
            }
        }

        /// <summary>
        /// The contact that receives the message.
        /// </summary>
        public string ToMail
        {
            get
            {
                return To.Replace("<msnmsgr:", String.Empty).Replace(">", String.Empty);
            }
        }

        public string Via
        {
            get
            {
                return mimeHeaders["Via"];
            }
            set
            {
                mimeHeaders["Via"] = value;
            }
        }

        /// <summary>
        /// The current branch this message applies to.
        /// </summary>
        public string Branch
        {
            get
            {
                return mimeHeaders["Via"]["branch"];
            }
            set
            {
                mimeHeaders["Via"]["branch"] = value;
            }
        }

        /// <summary>
        /// The sequence count of this message.
        /// </summary>
        public int CSeq
        {
            get
            {
                return int.Parse(mimeHeaders["CSeq"], System.Globalization.CultureInfo.InvariantCulture);
            }
            set
            {
                mimeHeaders["CSeq"] = value.ToString();
            }
        }

        public Guid CallId
        {
            get
            {
                return new Guid(mimeHeaders["Call-ID"]);
            }
            set
            {
                mimeHeaders["Call-ID"] = value.ToString("B").ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public string ContentType
        {
            get
            {
                return mimeHeaders["Content-Type"];
            }
            set
            {
                mimeHeaders["Content-Type"] = value;
            }
        }

        /// <summary>
        /// Contains all name/value combinations of non-header fields in the message
        /// </summary>
        public MimeDictionary BodyValues
        {
            get
            {
                return mimeBodies;
            }
        }

        /// <summary>
        /// Builds the entire message and returns it as a byte array. Ready to be used in a P2P Message.
        /// This function adds the 0x00 at the end of the message.
        /// </summary>
        /// <returns></returns>
        public override byte[] GetBytes()
        {
            string body = mimeBodies.ToString();

            // Update the Content-Length header, +1 the additional 0x00
            // mimeBodylength + \r\n\0
            mimeHeaders["Content-Length"] = (body.Length + 3).ToString();

            StringBuilder builder = new StringBuilder(512);
            builder.Append(StartLine);
            builder.Append("\r\n");
            builder.Append(mimeHeaders.ToString());
            builder.Append("\r\n");
            builder.Append(body);
            builder.Append("\r\n");

            // get the bytes			
            byte[] message = Encoding.GetBytes(builder.ToString());

            // add the additional 0x00
            byte[] totalMessage = new byte[message.Length + 1];
            message.CopyTo(totalMessage, 0);
            totalMessage[message.Length] = 0x00;

            return totalMessage;
        }

        /// <summary>
        /// Parses an MSNSLP message and stores the values in the object's fields.
        /// </summary>
        /// <param name="data">The messagedata to parse</param>			
        public override void ParseBytes(byte[] data)
        {
            int lineLen = MSNHttpUtility.IndexOf(data, "\r\n");
            byte[] lineData = new byte[lineLen];
            Array.Copy(data, lineData, lineLen);
            StartLine = Encoding.GetString(lineData);

            byte[] header = new byte[data.Length - lineLen - 2];
            Array.Copy(data, lineLen + 2, header, 0, header.Length);

            mimeHeaders.Clear();
            int mimeEnd = mimeHeaders.Parse(header);

            byte[] body = new byte[header.Length - mimeEnd];
            Array.Copy(header, mimeEnd, body, 0, body.Length);

            mimeBodies.Clear();
            mimeBodies.Parse(body);
        }

        /// <summary>
        /// Textual presentation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Encoding.GetString(GetBytes());
        }
    }
};
