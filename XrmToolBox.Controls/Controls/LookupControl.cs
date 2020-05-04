using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
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
        private bool selectingEntity = false;
        private bool changed = false;
        private Dictionary<string, Entity> cache = new Dictionary<string, Entity>();
        private static readonly Icon warningIcon;
        private static Icon errorIcon;

        static LookupControl()
        {
            var iconSize = SystemInformation.SmallIconSize;
            var bitmap = new Bitmap(iconSize.Width, iconSize.Height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(SystemIcons.Warning.ToBitmap(), new Rectangle(Point.Empty, iconSize));
            }

            warningIcon = Icon.FromHandle(bitmap.GetHicon());
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public LookupControl()
        {
            InitializeComponent();
            errorProvider.SetIconAlignment(txtLookup, ErrorIconAlignment.MiddleRight);
            errorProvider.SetIconPadding(txtLookup, -16);

            if (errorIcon == null)
            {
                errorIcon = errorProvider.Icon;
            }

            LinkVisible = true;
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
                selectingEntity = true;
                errorProvider.SetError(txtLookup, null);

                if (value == null)
                {
                    txtLookup.Text = "";
                }
                else
                {
                    txtLookup.Text = value.Name ?? value.Id.ToString("B");
                }

                ShowLink();

                changed = false;
                selectingEntity = false;
                SelectedItemChanged?.Invoke(this, new EventArgs());
            }
        }

        [DisplayName("Link Visible")]
        [Description("Show a valid selection as a hyperlink")]
        [Category("Display")]
        [DefaultValue(true)]
        public bool LinkVisible { get; set; }

        #endregion

        #region Event Definitions
        /// <summary>
        /// Event that fires when the Selected Item changes
        /// </summary>
        [Category("XrmToolBox")]
        [Description("Event that fires when the Selected Item in the Lookup changes")]
        public event EventHandler SelectedItemChanged;

        /// <summary>
        /// Event that fires when the Selected Item changes
        /// </summary>
        [Category("XrmToolBox")]
        [Description("Event that fires when the Selected Item in the Lookup is clicked")]
        public event EventHandler LinkClicked;
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

            cache.Clear();

            base.ClearData();
        }
        #endregion

        private void txtLookup_Leave(object sender, EventArgs e)
        {
            if (!changed)
            {
                ShowLink();
                return;
            }

            if (String.IsNullOrEmpty(txtLookup.Text))
            {
                SelectedEntity = null;
                errorProvider.SetError(txtLookup, null);
                return;
            }

            var results = AllowedEntities.Split(',').SelectMany(logicalName => LookupHelper.ExecuteQuickFind(Service, logicalName, txtLookup.Text, cache).Entities).ToList();

            if (results.Count == 0)
            {
                selectedEntity = null;
                SelectedItemChanged?.Invoke(this, new EventArgs());
                errorProvider.Icon = errorIcon;
                errorProvider.SetError(txtLookup, "No match found");

                ShowLink();
            }
            else if (results.Count > 1)
            {
                selectedEntity = null;
                SelectedItemChanged?.Invoke(this, new EventArgs());
                errorProvider.Icon = warningIcon;
                errorProvider.SetError(txtLookup, "Multiple matches found, click \"...\" to select record");

                ShowLink();
            }
            else
            {
                var entityReference = results[0].ToEntityReference();
                entityReference.Name = results[0].GetAttributeValue<string>(MetadataHelper.GetPrimaryAttribute(Service, entityReference.LogicalName).LogicalName);
                SelectedEntity = entityReference;
                errorProvider.SetError(txtLookup, null);
            }
        }

        private void btnLookup_Click(object sender, EventArgs e)
        {
            using (var form = new LookupSingleForm(AllowedEntities.Split(','), Service, cache, txtLookup.Text))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    SelectedEntity = form.SelectedEntity;
                }
            }
        }

        private void txtLookup_TextChanged(object sender, EventArgs e)
        {
            if (selectingEntity)
            {
                return;
            }

            changed = true;
        }

        private void ShowLink()
        {
            if (selectedEntity != null && LinkVisible)
            {
                txtLookup.ForeColor = SystemColors.HotTrack;
                txtLookup.Font = new Font(Font, Font.Style | FontStyle.Underline);
                txtLookup.Cursor = Cursors.Hand;
            }
            else
            {
                txtLookup.ForeColor = SystemColors.ControlText;
                txtLookup.Font = Font;
                txtLookup.Cursor = Cursors.IBeam;
            }
        }

        private void txtLookup_Click(object sender, EventArgs e)
        {
            if (selectedEntity != null && LinkVisible && ModifierKeys.HasFlag(Keys.Control))
                LinkClicked?.Invoke(this, new EventArgs());
        }

        private void txtLookup_Enter(object sender, EventArgs e)
        {
            txtLookup.ForeColor = SystemColors.ControlText;
            txtLookup.Font = Font;
            txtLookup.Cursor = Cursors.IBeam;
        }
    }
}
