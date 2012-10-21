using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;

using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Security;

namespace zmeng.SharePoint
{
    [CLSCompliant(false)]
    public class DigitalSignatureField : SPField
    {
        public DigitalSignatureField(SPFieldCollection fields, string fieldName)
            : base(fields, fieldName)
        {
        }

        public DigitalSignatureField(SPFieldCollection fields, string typeName, string displayName)
            : base(fields, typeName, displayName)
        {
        }

        public override BaseFieldControl FieldRenderingControl
        {
            [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true)]
            get
            {
                BaseFieldControl fieldControl = new DigitalSignatureFieldControl();
                fieldControl.FieldName = this.InternalName;

                return fieldControl;
            }
        }

        public override string GetFieldValueAsHtml(object value)
        {
            // This gets called for the version comparison
            return Utilities.RenderUserSet(this.ParentList.ParentWeb, value, null, Utilities.IconMode.SignedHidden, null);
        }
    }
}
