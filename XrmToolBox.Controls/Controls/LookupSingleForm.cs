using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections;
using System.Globalization;
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
            public Entity Entity { get; set; }

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
        private EntityMetadata metadata;

        public LookupSingleForm(string[] entityNames, IOrganizationService service, string search)
        {
            InitializeComponent();

            this.service = service;
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
            var entity = (Entity)lvResults.SelectedItems[0].Tag;
            SelectedEntity = entity.ToEntityReference();
            SelectedEntity.Name = entity.GetAttributeValue<string>(metadata.PrimaryNameAttribute);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnSearchClick(object sender, EventArgs e)
        {
            lvResults.Items.Clear();

            string newFetchXml = "";
            try
            {
                if (txtSearch.Text.Length == 0) txtSearch.Text = "*";

                var view = ((ViewInfo)cbbViews.SelectedItem).Entity;
                var layout = new XmlDocument();
                layout.LoadXml(view["layoutxml"].ToString());

                var result = LookupHelper.ExecuteQuickFind(service, LogicalName, view, txtSearch.Text);

                foreach (var entity in result.Entities)
                {
                    bool isFirstCell = true;

                    var item = new ListViewItem();
                    item.Tag = entity;

                    foreach (XmlNode cell in layout.SelectNodes("//cell"))
                    {
                        var attributeName = cell.Attributes["name"].Value;
                        if (!entity.FormattedValues.TryGetValue(attributeName, out var value))
                        {
                            if (entity.Attributes.TryGetValue(attributeName, out var rawValue))
                                value = rawValue?.ToString();

                            if (value == null)
                                value = "";
                        }

                        if (isFirstCell)
                        {
                            item.Text = value;
                            isFirstCell = false;
                        }
                        else
                        {
                            item.SubItems.Add(value);
                        }
                    }

                    lvResults.Items.Add(item);
                }

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
            lvResults.Columns.Clear();

            var view = ((ViewInfo)cbbViews.SelectedItem).Entity;
            var layout = new XmlDocument();
            layout.LoadXml(view["layoutxml"].ToString());

            foreach (XmlNode cell in layout.SelectNodes("//cell"))
            {
                var ch = new ColumnHeader();
                try
                {
                    ch.Text =
                        metadata.Attributes.First(a => a.LogicalName == cell.Attributes["name"].Value)
                            .DisplayName.UserLocalizedLabel.Label;
                    ch.Width = int.Parse(cell.Attributes["width"].Value);
                }
                catch
                {
                    ch.Text = cell.Attributes["name"].Value;
                }
                lvResults.Columns.Add(ch);
            }
        }

        private void CbbEntitiesSelectedIndexChanged(object sender, EventArgs e)
        {
            cbbViews.Items.Clear();
            SelectedEntity = null;

            var qe = new QueryExpression("savedquery");
            qe.ColumnSet = new ColumnSet(true);
            qe.Criteria.AddCondition("returnedtypecode", ConditionOperator.Equal, LogicalName);
            qe.Criteria.AddCondition("querytype", ConditionOperator.Equal, 4);
            var records = service.RetrieveMultiple(qe);

            if (records.Entities.Count == 0)
            {
                MessageBox.Show(this, "Cannot load views since this entity does not have Quick Find view defined", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int index = 0;
            int defaultViewIndex = 0;

            foreach (var record in records.Entities)
            {
                if ((bool)record["isdefault"])
                    defaultViewIndex = index;

                var view = new ViewInfo();
                view.Entity = record;

                cbbViews.Items.Add(view);

                index++;
            }

            cbbViews.SelectedIndex = defaultViewIndex;
            metadata = ((RetrieveEntityResponse)service.Execute(new RetrieveEntityRequest { LogicalName = LogicalName, EntityFilters = EntityFilters.Attributes })).EntityMetadata;
        }

        private void LvResultsColumnClick(object sender, ColumnClickEventArgs e)
        {
            lvResults.Sorting = lvResults.Sorting == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            lvResults.ListViewItemSorter = new ListViewItemComparer(e.Column, lvResults.Sorting);
        }

        private void LvResultsDoubleClick(object sender, EventArgs e)
        {
            BtnOkClick(null, null);
        }

        private void txtSearch_Enter(object sender, EventArgs e)
        {
            AcceptButton = btnSearch;
        }

        private void txtSearch_Leave(object sender, EventArgs e)
        {
            AcceptButton = btnOK;
        }

        private void LookupSingle_Load(object sender, EventArgs e)
        {
            btnSearch.Height = txtSearch.Height;
            btnSearch.Width = btnSearch.Height;
        }

        private void lvResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnOK.Enabled = lvResults.SelectedItems.Count == 1;
        }
    }
}