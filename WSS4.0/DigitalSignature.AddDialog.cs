using System;
using System.Data;
using System.Drawing;
using System.ComponentModel;
using System.Configuration;
using System.Collections;
using System.Text;
using System.IO;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Security;
using System.Reflection;
using System.Security.Principal;
using System.Security.Permissions;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace zmeng.SharePoint
{
    public partial class DigitalSignatureAddDialog : System.Web.UI.Page, System.Web.UI.ICallbackEventHandler
    {
        protected HtmlGenericControl body;
        protected PeopleEditor pedUser;
        protected String returnValue;

        protected void Page_Load(object sender, EventArgs e)
        {
            HtmlMeta metaExpiry = new HtmlMeta();
            metaExpiry.Name = "Expires";
            metaExpiry.Content = "0";
            Page.Header.Controls.Add(metaExpiry);

            RSACryptoServiceProvider rsa = Utilities.GetRSACryptoServiceProvider();
            RSAParameters rsaPublicParameters = rsa.ExportParameters(false);

            string cbReference = Page.ClientScript.GetCallbackEventReference(this, "arg", "ReceiveServerData", "context");
            string callbackScript = "function CallServer(arg, context)" + "{ " + cbReference + ";}";
            string spanCounterScript = "function getNumSpans(ctl) { var count = 0; for (var i=0; i<ctl.children.length; i++) if (ctl.children[i].tagName.toLowerCase() == 'span') count++; return count; }";
            string testPasswordAsyncScript = @"function TestPasswordAsync() { var ctlErrorMessage = document.getElementById('errorMessageDiv'); var ctlUsername = document.getElementById('pedUser_upLevelDiv'); var spanCount = getNumSpans(ctlUsername); if (spanCount == 0) ctlErrorMessage.innerHTML = 'No user selected'; else if (spanCount > 1) ctlErrorMessage.innerHTML = 'You can only select one user'; else { var username = ctlUsername.children[0].title; var password = document.getElementById('txtPassword').value; var reason = document.getElementById('ddlReason').value; ctlErrorMessage.innerHTML = ''; CallServer(username + ';' + EncryptPasswordForTransmission(password) + ';' + reason, ''); } }";
            string receiveServerDataScript = @"function ReceiveServerData(rValue) { if (rValue != ""x"" && rValue != ""?"" && rValue != """") { window.returnValue = rValue; window.close(); } else if (rValue == ""x"") { document.getElementById(""errorMessageDiv"").innerHTML = ""The password entered is incorrect. Please try again.""; document.getElementById(""usernameMessageDiv"").innerHTML == """"; var control = document.getElementById(""txtPassword""); control.value = """"; control.focus(); } else if (rValue == ""?"") { document.getElementById(""errorMessageDiv"").innerHTML = ""<br />""; document.getElementById(""usernameMessageDiv"").innerHTML = ""The username entered cannot be resolved.<br />Please try again.""; var control = document.getElementById(""txtPassword""); control.value = """";}}";
            string encryptPasswordScript = @"function EncryptPasswordForTransmission(unencrypted) { setMaxDigits(130); return encryptedString(new RSAKeyPair(rsaExponent, '', rsaModulus), unencrypted); }";
            string rsaParametersScript = String.Format("rsaExponent = '{0}'; rsaModulus = '{1}';",
                Utilities.BytesToHexString(rsaPublicParameters.Exponent),
                Utilities.BytesToHexString(rsaPublicParameters.Modulus)
                );

            IncludeScript("BigInt.js");
            IncludeScript("Barrett.js");
            IncludeScript("RSA.js");

            Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "SpanCounterScript", spanCounterScript, true);
            Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "CallServer", callbackScript, true);
            Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "TestPasswordAsync", testPasswordAsyncScript, true);
            Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "ReceiveServerData", receiveServerDataScript, true);
            Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "RSAParameters", rsaParametersScript, true);
            Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "EncryptPassword", encryptPasswordScript, true);

            Page.Title = "Add Digital Signature";

            HtmlLink styleLink = new HtmlLink();
            styleLink.Href = "/_layouts/1033/styles/core.css?rev=5msmprmeONfN6lJ3wtbAlA%3D%3D";
            styleLink.Attributes["type"] = "text/css";
            styleLink.Attributes["rel"] = "stylesheet";
            Page.Header.Controls.Add(styleLink);

            body.Attributes.Add("class", "ms-formbody");
        }

        private void IncludeScript(string src)
        {
            HtmlGenericControl include = new HtmlGenericControl("script");
            include.Attributes.Add("type", "text/javascript");
            include.Attributes.Add("src", src);
            this.Page.Header.Controls.Add(include);
        }

        protected override void CreateChildControls()
        {
            SPWeb spWeb = SPControl.GetContextWeb(this.Context);
            SPUser user = spWeb.CurrentUser;

            HtmlForm form = new HtmlForm();
            form.ID = "frmAddSignature";
            form.Method = "post";
            form.DefaultButton = "btnOK";
            body.Controls.Add(form);

            form.Controls.Add(new LiteralControl(@"<div style=""padding: 20px"">"));
            form.Controls.Add(new LiteralControl("Add your signature by entering your password<br /><br />"));
            form.Controls.Add(new LiteralControl(@"<div style=""margin-left: 20px"">"));

            Table table = new Table();
            form.Controls.Add(table);

            #region User row
            TableRow rowUser = new TableRow();
            table.Controls.Add(rowUser);

            TableCell cellUserLabel = new TableCell();
            cellUserLabel.Controls.Add(new LiteralControl(@"<h3 class=""ms-standardheader"" style=""font-size: 0.7em"">User </h3>"));
            rowUser.Controls.Add(cellUserLabel);

            TableCell cellUserControl = new TableCell();
            rowUser.Controls.Add(cellUserControl);

            pedUser = new PeopleEditor();
            pedUser.ID = "pedUser";
            pedUser.AllowEmpty = false;
            pedUser.AutoPostBack = true;
            pedUser.SelectionSet = "User";
            pedUser.Width = Unit.Pixel(200);
            pedUser.PlaceButtonsUnderEntityEditor = false;
            pedUser.Rows = 1;
            pedUser.MultiSelect = false;
            pedUser.ValidatorEnabled = true;
            pedUser.CommaSeparatedAccounts = user.ToString();
            cellUserControl.Controls.Add(pedUser);

            TableRow rowUsernameValidation = new TableRow();
            table.Controls.Add(rowUsernameValidation);

            TableCell cellUsernameValidationSpacer = new TableCell();
            rowUsernameValidation.Controls.Add(cellUsernameValidationSpacer);

            TableCell cellUsernameValidation = new TableCell();
            cellUsernameValidation.Controls.Add(new LiteralControl(@"<div id=""usernameMessageDiv"" class=""ms-formvalidation""></div>"));
            rowUsernameValidation.Controls.Add(cellUsernameValidation);
            #endregion

            #region Reason row
            TableRow rowReason = new TableRow();
            table.Controls.Add(rowReason);

            TableCell cellReasonLabel = new TableCell();
            cellReasonLabel.Controls.Add(new LiteralControl(@"<h3 class=""ms-standardheader"" style=""font-size: 0.7em"">Reason </h3>"));
            rowReason.Controls.Add(cellReasonLabel);

            TableCell cellReasonControl = new TableCell();
            rowReason.Controls.Add(cellReasonControl);

            DropDownList ddlReason = new DropDownList();
            ddlReason.ID = "ddlReason";
            ddlReason.AutoPostBack = false;
            ddlReason.Items.Add(new ListItem("Approved"));
            ddlReason.Items.Add(new ListItem("Reviewed"));
            ddlReason.Items.Add(new ListItem("Rejected"));
            ddlReason.SelectedIndex = 0;
            ddlReason.BorderWidth = Unit.Pixel(1);
            ddlReason.BorderColor = Color.FromArgb(165, 165, 165);
            ddlReason.BorderStyle = BorderStyle.Solid;
            cellReasonControl.Controls.Add(ddlReason);

            /*
            TextBox txtReason = new TextBox();
            txtReason.ID = "txtReason";
            txtReason.Width = Unit.Pixel(250);
            txtReason.BorderWidth = Unit.Pixel(1);
            txtReason.BorderColor = Color.FromArgb(165, 165, 165);
            txtReason.BorderStyle = BorderStyle.Solid;
            cellReasonControl.Controls.Add(txtReason); 
            */
            #endregion

            #region Password row
            TableRow rowPassword = new TableRow();
            table.Controls.Add(rowPassword);

            TableCell cellPasswordLabel = new TableCell();
            cellPasswordLabel.Controls.Add(new LiteralControl(@"<h3 class=""ms-standardheader"" style=""font-size: 0.7em"">Password </h3>"));
            rowPassword.Controls.Add(cellPasswordLabel);

            TableCell cellPasswordControl = new TableCell();
            rowPassword.Controls.Add(cellPasswordControl);

            TextBox txtPassword = new TextBox();
            txtPassword.ID = "txtPassword";
            txtPassword.TextMode = TextBoxMode.Password;
            txtPassword.Width = Unit.Pixel(157);
            txtPassword.BorderWidth = Unit.Pixel(1);
            txtPassword.BorderColor = Color.FromArgb(165, 165, 165);
            txtPassword.BorderStyle = BorderStyle.Solid;
            cellPasswordControl.Controls.Add(txtPassword);

            TableRow rowPasswordValidation = new TableRow();
            table.Controls.Add(rowPasswordValidation);

            TableCell cellPasswordValidationSpacer = new TableCell();
            rowPasswordValidation.Controls.Add(cellPasswordValidationSpacer);

            TableCell cellPasswordValidation = new TableCell();
            cellPasswordValidation.Controls.Add(new LiteralControl(@"<div id=""errorMessageDiv"" class=""ms-formvalidation""><br /></div>"));
            rowPasswordValidation.Controls.Add(cellPasswordValidation);
            #endregion

            form.Controls.Add(new LiteralControl(@"</div>"));

            form.Controls.Add(new LiteralControl(@"<div style=""position: absolute; bottom: 20px; right: 24px;"">"));

            Button btnOK = new Button();
            btnOK.ID = "btnOK";
            btnOK.Text = "OK";
            btnOK.Width = new Unit(64, UnitType.Pixel);
            btnOK.OnClientClick = @"javascript:TestPasswordAsync(); return false;";
            form.Controls.Add(btnOK);

            form.Controls.Add(new LiteralControl("&nbsp;"));

            Button btnCancel = new Button();
            btnCancel.ID = "btnCancel";
            btnCancel.Text = "Cancel";
            btnCancel.Width = new Unit(64, UnitType.Pixel);
            btnCancel.OnClientClick = @"javascript:window.close(); return false;";
            form.Controls.Add(btnCancel);

            form.Controls.Add(new LiteralControl(@"</div>"));
            form.Controls.Add(new LiteralControl(@"</div>"));

            body.Attributes.Add("onload", "document.getElementById('txtPassword').focus();");

            base.CreateChildControls();
        }


        public void RaiseCallbackEvent(String eventArgument)
        {
            SPWeb spWeb = SPControl.GetContextWeb(this.Context);

            string[] tokenized = eventArgument.Split(';');
            string username = tokenized[0].Trim();
            string encryptedPassword = tokenized[1].Trim();
            string reason = tokenized[2].Trim();

            try
            {
                SPUser user = spWeb.SiteUsers[username];
                if (user == null)
                    throw new Exception();
            }
            catch
            {
                returnValue = "?";
                return;
            }

            string password = "";
            try
            {
                RSACryptoServiceProvider rsa = Utilities.GetRSACryptoServiceProvider();
                byte[] binPassword = rsa.Decrypt(Utilities.HexStringToBytes(encryptedPassword), false);
                password = ASCIIEncoding.ASCII.GetString(binPassword);
            }
            catch (Exception ex)
            {
#if DEBUG
                throw ex;
#endif
            }

            bool passwordGood = Utilities.TestPassword(username, password);

            string cleanReason = reason.Trim().Replace(",", "&#44;");

            if (passwordGood)
                returnValue = "+" + username + ((cleanReason.Length > 0) ? ":" + cleanReason : "");
            else
                returnValue = "x";
        }

        public String GetCallbackResult()
        {
            return returnValue;
        }


        private Guid GetAssemblyGuid()
        {
            try
            {
                Assembly assembly = Assembly.Load("zmeng.SharePoint.DigitalSignature, Version=1.1.0.0, Culture=neutral, PublicKeyToken=d19b49a3056a8fa1");
                object[] customAttributes = assembly.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);

                if (customAttributes.Length == 0)
                    return Guid.Empty;

                return new Guid(((GuidAttribute)customAttributes[0]).Value);
            }
            catch
            {
                return Guid.Empty;
            }
        }
    }
}