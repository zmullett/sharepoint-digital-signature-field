using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using System.Security.Principal;

namespace zmeng.SharePoint
{
    [CLSCompliant(false)]
	public class DigitalSignatureFieldControl : BaseFieldControl
    {
		private LinkButton btnAddSignature;
		private StringBuilder debug = new StringBuilder();

		public override object Value
		{
			get
			{
				this.EnsureChildControls();

				string newContent = "";
				if (this.ViewState[this.UniqueID] != null)
					newContent = (string)this.ViewState[this.UniqueID];

				if (newContent.Length > 0)
				{
					bool append = false;

					string content = newContent;

					// This won't work properly - old hashes
					// need to be removed first. Also, what
					// should be done about appending valid signatures
					// to signatures with invalid hashes?
					if (append)
						content = this.ItemFieldValue + "," + newContent;

					HasherType1 hasher = new HasherType1();

					Guid itemId = new Guid();
					try { itemId = this.ListItem.UniqueId; }
					catch 
					{ 
						// If signature is added to a new record
						// then it won't yet have an ID (of any kind)
						// associated with it, so set the hasher options
						// to exclude the item's ID
						if ((hasher.Options & HasherType1.OptionsFlags.HashItemId) == HasherType1.OptionsFlags.HashItemId)
							hasher.Options ^= HasherType1.OptionsFlags.HashItemId;
					}

					HasherType1.Seed seed = new HasherType1.Seed(content, itemId);

					return content + " (" + hasher.CalculateHash(seed) + ")";
				}
				else
					return this.ItemFieldValue;
			}
			set { }
		}

		protected override void CreateChildControls()
		{
			bool editing = this.ControlMode == SPControlMode.Edit 
				|| this.ControlMode == SPControlMode.New;

			if (editing)
			{
				btnAddSignature = new LinkButton();
				btnAddSignature.Text = "Add Signature";
				btnAddSignature.Style.Add(HtmlTextWriterStyle.Display, "block");
				btnAddSignature.OnClientClick = "AddDigitalSignature('" + this.UniqueID + "'); return false;";
				this.Controls.Add(btnAddSignature);
			}
		}

		protected override void OnPreRender(EventArgs e)
		{
			if (this.Page.IsPostBack)
			{
				if (!string.IsNullOrEmpty(this.Page.Request.Form["__EVENTTARGET"]))
				{
					if (this.Page.Request.Form["__EVENTTARGET"].Equals(this.UniqueID))
					{
						string value = this.Page.Request.Form["__EVENTARGUMENT"];
						string content = value.Substring(1);	// First character is + or - command

						try
						{
							string login = content;
							

							//SPWeb spWeb = SPControl.GetContextWeb(this.Context);
							//SPUser spUser = spWeb.SiteUsers[content];

							if (value[0].Equals('+'))
								UpdateContentItems(content, true);
							else if (value[0].Equals('-'))
								UpdateContentItems(content, false);
						}
						catch
						{
						}
					}
				}
			}

			string postBackScript = "function AddDigitalSignature(id) {"
				+ "var retValue = window.showModalDialog(\"/_layouts/zmeng/DigitalSignature/AddDigitalSignature.aspx\",\"DigitalSignatureWindow\",\"dialogWidth=470px;dialogHeight=214px;dialogHide:true;help:no;scroll:no\"); "
				+ "if (retValue != null && retValue != \"\")"
				+ " __doPostBack(id, retValue);"
				+ "}";
			this.Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "DigitalSignature", postBackScript, true);

			base.OnPreRender(e);
		}

		private void UpdateContentItems(string content, bool add)
		{
			string strExistingContentItems = "";
			if (this.ViewState[this.UniqueID] != null)
				strExistingContentItems = (string)this.ViewState[this.UniqueID];

			string[] split = strExistingContentItems.Split(',');
			List<string> newContentItems = new List<string>();
			foreach (string existingContentItem in split)
			{
				// Remove any previous instances
				if (!existingContentItem.Equals(content))
					newContentItems.Add(existingContentItem);
			}

			if (add)
				newContentItems.Add(content);

			string strNewContentItems = "";
			foreach (string newContentItem in newContentItems)
			{
				if (strNewContentItems.Length > 0)
					strNewContentItems += ",";
				strNewContentItems += newContentItem;
			}

			this.ViewState[this.UniqueID] = strNewContentItems;
		}

		protected override void RenderFieldForDisplay(HtmlTextWriter output)
		{
			SPListItemVersion requestedVersion = Utilities.GetRequestedVersion(this);
			output.Write(Utilities.RenderSignatureBlock(requestedVersion, this.FieldName, true));
		}
		protected override void RenderFieldForInput(HtmlTextWriter output)
		{
			#if DEBUG
			output.Write("DEBUG " + debug.ToString());


			#endif

			string existingSignatures = "";
			if (this.ControlMode == SPControlMode.Edit)
			{
				SPListItemVersion requestedVersion = Utilities.GetRequestedVersion(this);
				existingSignatures = Utilities.RenderSignatureBlock(requestedVersion, this.FieldName, true);
				output.Write(existingSignatures);
			}

			string newContentItems = "";
			if (this.ViewState[this.UniqueID] != null)
				newContentItems = (string)this.ViewState[this.UniqueID];

			if (newContentItems.Length > 0)
			{
				if (existingSignatures.Length > 0)
					output.Write("<br/>");

				output.Write(Utilities.RenderUserSet(this.Web, newContentItems, null, Utilities.IconMode.SignedNew, this.UniqueID));
			}
			else
			{
				if (existingSignatures.Length > 0 | newContentItems.Length > 0)
					output.Write("<br/>");

				btnAddSignature.RenderControl(output);
			}
		}
	}
}
