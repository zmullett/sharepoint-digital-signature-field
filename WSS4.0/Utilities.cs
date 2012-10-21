using System;
using System.Security.Cryptography;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.UI;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;
using System.Diagnostics;

namespace zmeng.SharePoint
{
    class Utilities
    {
        #region enum IconMode
        public enum IconMode
        {
            SignedUnversioned = 1,
            SignedCurrent = 2,
            SignedPrevious = 4,
            SignedHidden = 8,
            SignedNew = 16,
            Invalid = 32
        }
        #endregion

        public static string RenderUserSet(SPWeb spWeb, object value, string formattedDate, IconMode iconMode, string deleteLinkControlId)
        {
            if (!(value is string))
                return "";

            string s = (string)value;

            if (s.Trim().Length == 0)
                return "";

            int hashStart = s.IndexOf("(");
            if (hashStart == -1)
                hashStart = s.Length + 1;

            string[] contentItems = s.Substring(0, hashStart - 1).Split(',');

            StringBuilder sb = new StringBuilder();
            sb.Append("<table>");
            bool first = true;
            foreach (string contentItem in contentItems)
            {
                string login = contentItem;
                string reason = null;

                int separatorIndex = contentItem.IndexOf(':');
                if (separatorIndex != -1)
                {
                    login = contentItem.Substring(0, separatorIndex);
                    reason = contentItem.Substring(separatorIndex + 1);
                }

                sb.Append("<tr><td class=\"ms-vb\">");
                if (first)
                {
                    sb.Append(RenderIcon(iconMode));
                    first = false;
                }
                sb.Append("</td><td class=\"ms-vb\"><span><nobr>");

                try
                {
                    SPUser spUser = spWeb.SiteUsers[login.Trim()];
                    if (spUser != null)
                        sb.Append(RenderUser(spUser));
                    else
                        throw new Exception();	// Render in catch
                }
                catch
                {
                    sb.Append(login.Trim());
                }

                if (formattedDate != null)
                    sb.Append(" " + formattedDate);

                if (reason != null)
                    sb.Append(": " + reason);

                if (deleteLinkControlId != null)
                {
                    string escapedLoginName = contentItem.Trim().Replace("\\", "\\\\");
                    sb.Append(" (<a href=\"javascript:__doPostBack('" + deleteLinkControlId + "','-" + escapedLoginName + "');\">undo</a>)");
                }

                sb.Append("</span></nobr></td></tr>");
            }
            sb.Append("</table>");

            return sb.ToString();
        }
        public static string RenderUser(SPUser user)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<A HREF=\"");
            sb.Append("/_layouts/userdisp.aspx");
            sb.Append("?ID=");
            sb.Append(user.ID);
            sb.Append("\">");
            sb.Append(SPHttpUtility.HtmlEncode(user.Name));
            sb.Append("</A>");
            //sb.Append("<img border=\"0\" height=\"1\" width=\"3\" src=\"/_layouts/images/blank.gif\"/>");

            return sb.ToString();
        }
        public static string RenderIcon(IconMode iconMode)
        {
            if ((iconMode & IconMode.SignedHidden) == IconMode.SignedHidden)
                return "";

            if ((iconMode & IconMode.SignedNew) == IconMode.SignedNew)
            {
                return String.Format("<img src=\"/_layouts/images/zmeng/DigitalSignature/{0}\" alt=\"{1}\" title=\"{1}\" />"
                    + "<img src=\"/_layouts/images/zmeng/DigitalSignature/{2}\" alt=\"{1}\" title=\"{1}\" />",
                "signed.gif",
                "New signature",
                "new.gif");
            }

            string s = "";

            if ((iconMode & IconMode.Invalid) == IconMode.Invalid)
            {
                s += String.Format("<img src=\"/_layouts/images/zmeng/DigitalSignature/{0}\" alt=\"{1}\" title=\"{1}\" />",
                    "invalid.gif",
                    "The checksum of this signature is invalid");
            }

            string icon = "signed.gif";
            string toolTip = "Signed";

            if ((iconMode & IconMode.SignedCurrent) == IconMode.SignedCurrent)
                toolTip = "Current version signed";
            else if ((iconMode & IconMode.SignedPrevious) == IconMode.SignedPrevious)
            {
                icon = "signed-previous.gif";
                toolTip = "Previous version signed";
            }

            s += String.Format("<img src=\"/_layouts/images/zmeng/DigitalSignature/{0}\" alt=\"{1}\" title=\"{1}\" />",
                icon, toolTip);

            return s;
        }
        public static string RenderSignatureBlock(SPListItemVersion version, string fieldDisplayName, bool showVersionInformationText)
        {
            StringBuilder sb = new StringBuilder();

            SPListItemVersion signedVersion = Utilities.GetVersionSignedSince(version, fieldDisplayName);

            if (signedVersion != null)
            {
                string value = (string)signedVersion[fieldDisplayName];

                Utilities.IconMode invalidFlag = Utilities.IconMode.Invalid;
                try
                {
                    int hashIndexStart = value.IndexOf("(");
                    int hashIndexEnd = value.IndexOf(")", hashIndexStart);
                    string hash = value.Substring(hashIndexStart + 1, hashIndexEnd - hashIndexStart - 1);

                    string content = value.Substring(0, hashIndexStart).Trim();

                    HasherType1 hasher = new HasherType1();
                    HasherType1.Seed seed = new HasherType1.Seed(content, version.ListItem.UniqueId);
                    if (hasher.ValidateHash(seed, hash))
                        invalidFlag = 0;
                }
                catch
                { }

                bool listIsVersioned = version.ListItem.ParentList.EnableVersioning;

                if (!listIsVersioned)
                    sb.Append(Utilities.RenderUserSet(version.ListItem.Web, value, FormatTimestamp(version.Created, showVersionInformationText), Utilities.IconMode.SignedUnversioned | invalidFlag, null));
                else if (signedVersion.VersionId == version.VersionId)
                    sb.Append(Utilities.RenderUserSet(version.ListItem.Web, value, FormatTimestamp(version.Created, showVersionInformationText), Utilities.IconMode.SignedCurrent | invalidFlag, null));
                else
                {
                    if (showVersionInformationText)
                    {
                        sb.Append(Utilities.RenderUserSet(version.ListItem.Web, value, null, Utilities.IconMode.SignedPrevious | invalidFlag, null));
                        sb.Append("Signed " + Utilities.FormatTimestamp(signedVersion.Created, true) + " in <a href=\"DispForm.aspx?ID=" + version.ListItem.ID + "&VersionNo=" + signedVersion.VersionId + "\">version " + signedVersion.VersionLabel + "</a>.<br/>");
                    }
                    else
                    {
                        // Show timestamp anyway, but not full versioning info
                        sb.Append(Utilities.RenderUserSet(version.ListItem.Web, value, FormatTimestamp(version.Created, false), Utilities.IconMode.SignedPrevious | invalidFlag, null));
                    }
                }
            }

            return sb.ToString();
        }
        public static string FormatTimestamp(DateTime versionTimestamp, bool includeTime)
        {
            // Converts to local time (if in universal),
            // formats by server culture date rules

            if (includeTime)
            {
                // Exclude seconds for clarity
                DateTimeFormatInfo dtfi = (DateTimeFormatInfo)CultureInfo.CurrentCulture.DateTimeFormat.Clone();
                dtfi.LongTimePattern = "h:mm tt";
                return versionTimestamp.ToLocalTime().ToString(dtfi);
            }
            else
                return versionTimestamp.ToLocalTime().ToString("d");
        }
        public static SPListItemVersion GetVersionSignedSince(SPListItemVersion thisVersion, string fieldDisplayName)
        {
            // The current version has no signature
            if (thisVersion[fieldDisplayName] == null || thisVersion[fieldDisplayName].ToString().Trim() == "")
                return null;

            bool foundVersionOfInterest = false;
            SPListItemVersion pastVersion = null;
            foreach (SPListItemVersion version in thisVersion.ListItem.Versions)
            {
                // Skip through until reached version
                // of interest
                if (!foundVersionOfInterest)
                {
                    if (version.VersionId == thisVersion.VersionId)
                    {
                        foundVersionOfInterest = true;
                        pastVersion = version;
                    }
                    continue;
                }

                // Now start comparing data until
                // the data has changed. This seems
                // to be the only way to test when the
                // field was updated
                if (!version.Fields.ContainsField(fieldDisplayName))
                {
                    // The field didn't exist, maybe
                    // temporarily. So skip this version
                    // and don't advance pastVersion
                    continue;
                }

                if (pastVersion != null)
                {
                    //version[this.FieldName].Equals(pastVersion[this.FieldName]
                    string thisVersionData = "";
                    if (version[fieldDisplayName] != null)
                        thisVersionData = version[fieldDisplayName].ToString();

                    string pastVersionData = "";
                    if (pastVersion[fieldDisplayName] != null)
                        pastVersionData = pastVersion[fieldDisplayName].ToString();

                    if (!thisVersionData.Equals(pastVersionData))
                    {
                        // Data differs, so return the last
                        // version for which it was the same
                        return pastVersion;
                    }
                }

                pastVersion = version;
            }

            return pastVersion;
        }
        public static SPListItemVersion GetRequestedVersion(BaseFieldControl fieldControl)
        {
            string s = fieldControl.Page.Request.QueryString["VersionNo"];

            if (s != null)
            {
                try
                {
                    int versionId = Int32.Parse(s);
                    return fieldControl.ListItem.Versions.GetVersionFromID(versionId);
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                // No version specified so return 
                // most current version
                return fieldControl.ListItem.Versions[0];
            }
        }
        public static RSACryptoServiceProvider GetRSACryptoServiceProvider()
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();

            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                string propertyName = "zmeng.DigitalSignature.TransmissionKey";
                using (SPWeb spWeb = SPContext.Current.Web)
                {
                    if (spWeb.AllProperties.ContainsKey(propertyName))
                        rsa.FromXmlString(spWeb.AllProperties[propertyName] as string);
                    else
                    {
                        spWeb.AllowUnsafeUpdates = true;
                        spWeb.AllProperties.Add(propertyName, rsa.ToXmlString(true));
                        spWeb.Update();
                        spWeb.AllowUnsafeUpdates = false;
                    }
                }
            });

            return rsa;
        }
        public static string BytesToHexString(byte[] ba)
        {
            StringBuilder sb = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }

        public static byte[] HexStringToBytes(String hex)
        {
            int numChars = hex.Length;
            byte[] bytes = new byte[numChars / 2];
            for (int i = 0; i < numChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
        public static void WriteToLog(string message, EventLogEntryType eventEntryType)
        {
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                EventLog.WriteEntry("zmeng.SharePoint.DigitalSignature", message, eventEntryType);
            });
        }

        #region Password test definitions
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool LogonUser(
            string lpszUsername,
            string lpszDomain,
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            out IntPtr phToken
            );

        public static bool TestPassword(string qualifiedUsername, string password)
        {
            #if DEBUG
            //Utilities.WriteToLog(String.Format("TestPassword: {0}, {1}", qualifiedUsername, password), System.Diagnostics.EventLogEntryType.Information);
            #endif

            const int LOGON32_LOGON_NETWORK = 3;
            const int LOGON32_PROVIDER_DEFAULT = 0;

            string domain = "";
            string username;

            int slashIndex = qualifiedUsername.IndexOf('\\');
            if (slashIndex >= 0)
            {
                domain = qualifiedUsername.Substring(0, slashIndex);
                username = qualifiedUsername.Substring(slashIndex + 1, qualifiedUsername.Length - slashIndex - 1);
            }
            else
                username = qualifiedUsername;

            IntPtr hToken;
            bool logonResult = LogonUser(username, domain, password,
                LOGON32_LOGON_NETWORK,
                LOGON32_PROVIDER_DEFAULT,
                out hToken);

            return logonResult;
        }
        #endregion
    }
}
