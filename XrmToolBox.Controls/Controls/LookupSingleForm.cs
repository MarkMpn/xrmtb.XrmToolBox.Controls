using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using xrmtb.XrmToolBox.Controls.Helper;

namespace xrmtb.XrmToolBox.Controls
{
    public partial class LookupSingleForm : Form
    {
        class ViewInfo
        {
            public ViewInfo(Entity view)
            {
                Entity = view;
            }

            public Entity Entity { get; }

            public override string ToString()
            {
                return Entity["name"].ToString();
            }
        }

        /// <summary>
        /// Compares two listview items for sorting
        /// </summary>
        internal class ListViewItemComparer : IComparer
        {
            #region Variables

            /// <summary>
            /// Index of sorting column
            /// </summary>
            private readonly int col;

            /// <summary>
            /// Sort order
            /// </summary>
            private readonly SortOrder innerOrder;

            #endregion Variables

            #region Constructors

            /// <summary>
            /// Initializes a new instance of class ListViewItemComparer
            /// </summary>
            public ListViewItemComparer()
            {
                col = 0;
                innerOrder = SortOrder.Ascending;
            }

            /// <summary>
            /// Initializes a new instance of class ListViewItemComparer
            /// </summary>
            /// <param name="column">Index of sorting column</param>
            /// <param name="order">Sort order</param>
            public ListViewItemComparer(int column, SortOrder order)
            {
                col = column;
                innerOrder = order;
            }

            #endregion Constructors

            #region Methods

            /// <summary>
            /// Compare tow objects
            /// </summary>
            /// <param name="x">object 1</param>
            /// <param name="y">object 2</param>
            /// <returns></returns>
            public int Compare(object x, object y)
            {
                return Compare((ListViewItem)x, (ListViewItem)y);
            }

            /// <summary>
            /// Compare tow listview items
            /// </summary>
            /// <param name="x">Listview item 1</param>
            /// <param name="y">Listview item 2</param>
            /// <returns></returns>
            public int Compare(ListViewItem x, ListViewItem y)
            {
                if (innerOrder == SortOrder.Ascending)
                {
                    return String.CompareOrdinal(x.SubItems[col].Text, y.SubItems[col].Text);
                }

                return String.CompareOrdinal(y.SubItems[col].Text, x.SubItems[col].Text);
            }

            #endregion Methods
        }

        private readonly IOrganizationService service;
        private readonly IDictionary<string, Entity> cache;
        private EntityMetadata metadata;

        public LookupSingleForm(string[] entityNames, IOrganizationService service, IDictionary<string,Entity> cache, string search)
        {
            InitializeComponent();

            gvResults.OrganizationService = service;

            this.service = service;
            this.cache = cache;
            cbbEntities.Items.AddRange(entityNames);

            cbbEntities.SelectedIndex = 0;

            if (!String.IsNullOrEmpty(search))
            {
                txtSearch.Text = search;
                BtnSearchClick(btnSearch, EventArgs.Empty);
            }
        }

        public string LogicalName => (string)cbbEntities.SelectedItem;

        public EntityReference SelectedEntity { get; private set; }

        private void BtnCancelClick(object sender, EventArgs e)
        {
            SelectedEntity = null;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void BtnOkClick(object sender, EventArgs e)
        {
            var entity = gvResults.SelectedRowRecords[0];
            SelectedEntity = entity.ToEntityReference();
            SelectedEntity.Name = entity.GetAttributeValue<string>(MetadataHelper.GetPrimaryAttribute(service, LogicalName).LogicalName);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnSearchClick(object sender, EventArgs e)
        {
            string newFetchXml = "";
            try
            {
                if (txtSearch.Text.Length == 0) txtSearch.Text = "*";

                var view = ((ViewInfo)cbbViews.SelectedItem).Entity;

                var result = LookupHelper.ExecuteQuickFind(service, LogicalName, view, txtSearch.Text);
                gvResults.DataSource = result;

                ApplyColumnOrder();

                if (result.MoreRecords)
                {
                    MessageBox.Show(this,
                        "There is more than 250 records that match your search! Please refine your search",
                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

            }
            catch (Exception error)
            {
                MessageBox.Show(this,
                    "An error occured: " + error.ToString() + " --> " + newFetchXml,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CbbViewsSelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyColumnOrder();
        }

        private void ApplyColumnOrder()
        {
            var view = ((ViewInfo)cbbViews.SelectedItem).Entity;
            var layout = new XmlDocument();
            layout.LoadXml(view["layoutxml"].ToString());
            gvResults.ColumnOrder = String.Join(",", layout.SelectNodes("//cell/@name").OfType<XmlAttribute>().Select(a => a.Value));
        }

        private void CbbEntitiesSelectedIndexChanged(object sender, EventArgs e)
        {
            cbbViews.Items.Clear();
            SelectedEntity = null;

            Entity view;

            try
            {
                view = LookupHelper.LoadQuickFindView(service, LogicalName, cache);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            cbbViews.Items.Add(new ViewInfo(view));
            cbbViews.SelectedIndex = 0;
            metadata = MetadataHelper.GetEntity(service, LogicalName);
        }

        private void txtSearch_Enter(object sender, EventArgs e)
        {
            AcceptButton = btnSearch;
        }

        private void txtSearch_Leave(object sender, EventArgs e)
        {
            AcceptButton = btnOK;
        }

        private void gvResults_SelectionChanged(object sender, EventArgs e)
        {
            btnOK.Enabled = gvResults.SelectedRowRecords.Entities.Count == 1;
        }

        private void gvResults_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            BtnOkClick(null, null);
        }
    }
}