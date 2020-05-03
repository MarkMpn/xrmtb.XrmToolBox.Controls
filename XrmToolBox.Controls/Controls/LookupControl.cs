using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Xrm.Sdk;
using xrmtb.XrmToolBox.Controls.Helper;

namespace xrmtb.XrmToolBox.Controls
{
    /// <summary>
    /// Shared XrmToolBox Control that will load a list of entities into a Dropdown control
    /// </summary>
    public partial class LookupControl : XrmToolBoxControlBase
    {
        private EntityReference selectedEntity = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public LookupControl()
        {
            InitializeComponent();
        }

        #region Public Properties

        [DisplayName("Allowed Entities")]
        [Description("Comma separated list of entity logical names that the user can select from.")]
        [Category("Data")]
        [DefaultValue(null)]
        public string AllowedEntities { get; set; }

        /// <summary>
        /// The currently selected EntityMetadata object in the ListView
        /// </summary>
        [DisplayName("Selected Entity")]
        [Description("The Entity that is currently selected in the Lookup.")]
        [Category("XrmToolBox")]
        [Browsable(false)]
        public EntityReference SelectedEntity
        {
            get => selectedEntity;
            set
            {
                selectedEntity = value;

                if (value == null)
                {
                    txtLookup.Text = "";
                }
                else
                {
                    txtLookup.Text = value.Name ?? value.Id.ToString("B");
                }
                SelectedItemChanged?.Invoke(this, new EventArgs());
            }
        }
        #endregion

        #region Event Definitions
        /// <summary>
        /// Event that fires when the Selected Item changes
        /// </summary>
        [Category("XrmToolBox")]
        [Description("Event that fires when the Selected Item in the Dropdown changes")]
        public event EventHandler SelectedItemChanged;
        #endregion


        #region IXrmToolBoxControl methods
        /// <summary>
        /// Clear all loaded data in your control
        /// </summary>
        public override void ClearData()
        {
            OnBeginClearData();

            if (SelectedEntity != null)
            {
                SelectedEntity = null;
                SelectedItemChanged?.Invoke(this, new EventArgs());
            }

            base.ClearData();
        }
        #endregion

        private void txtLookup_TextChanged(object sender, EventArgs e)
        {
            var results = AllowedEntities.Split(',').SelectMany(logicalName => LookupHelper.ExecuteQuickFind(Service, logicalName, txtLookup.Text).Entities).ToList();

            if (results.Count == 0)
            {
                errorProvider.SetError(txtLookup, "No match found");
            }
            else if (results.Count > 1)
            {
                errorProvider.SetError(txtLookup, "Multiple matches found, click \"...\" to select record");
            }
            else
            {
                var entityReference = results[0].ToEntityReference();
                entityReference.Name = results[0].GetAttributeValue<string>(MetadataHelper.GetPrimaryAttribute(Service, entityReference.LogicalName).LogicalName);
                SelectedEntity = entityReference;
            }
        }

        private void btnLookup_Click(object sender, EventArgs e)
        {
            using (var form = new LookupSingleForm(AllowedEntities.Split(','), Service, txtLookup.Text))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    SelectedEntity = form.SelectedEntity;
                }
            }
        }
    }
}
