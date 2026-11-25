using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using Oqtane.Migrations.EntityBuilders;

namespace GIBS.Module.FBClients.Lists.Migrations.EntityBuilders
{
    public class ListsEntityBuilder : AuditableBaseEntityBuilder<ListsEntityBuilder>
    {
        private const string _entityTableName = "GIBSFBClientsLists";
        private readonly PrimaryKey<ListsEntityBuilder> _primaryKey = new("PK_GIBSFBClientsLists", x => x.ListsId);
        private readonly ForeignKey<ListsEntityBuilder> _moduleForeignKey = new("FK_GIBSFBClientsLists_Module", x => x.ModuleId, "Module", "ModuleId", ReferentialAction.Cascade);

        public ListsEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = _entityTableName;
            PrimaryKey = _primaryKey;
            ForeignKeys.Add(_moduleForeignKey);
        }

        protected override ListsEntityBuilder BuildTable(ColumnsBuilder table)
        {
            ListsId = AddAutoIncrementColumn(table,"ListsId");
            ModuleId = AddIntegerColumn(table,"ModuleId");
            Name = AddMaxStringColumn(table,"Name");
            ListName = AddStringColumn(table, "ListName", 100);
            Value = AddStringColumn(table, "Value", 300);
            Text = AddStringColumn(table, "Text", 150);
            ParentID = AddIntegerColumn(table, "ParentID");
            Level = AddIntegerColumn(table, "Level");
            SortOrder = AddIntegerColumn(table, "SortOrder");
            DefinitionID = AddIntegerColumn(table, "DefinitionID");
            Description = AddStringColumn(table, "Description", 500, true);
            SiteID = AddIntegerColumn(table, "SiteID");
            SystemList = AddBooleanColumn(table, "SystemList");
            AddAuditableColumns(table);
            return this;
        }

        public OperationBuilder<AddColumnOperation> ListsId { get; set; }
        public OperationBuilder<AddColumnOperation> ModuleId { get; set; }
        public OperationBuilder<AddColumnOperation> Name { get; set; }
        public OperationBuilder<AddColumnOperation> ListName { get; set; }
        public OperationBuilder<AddColumnOperation> Value { get; set; }
        public OperationBuilder<AddColumnOperation> Text { get; set; }
        public OperationBuilder<AddColumnOperation> ParentID { get; set; }
        public OperationBuilder<AddColumnOperation> Level { get; set; }
        public OperationBuilder<AddColumnOperation> SortOrder { get; set; }
        public OperationBuilder<AddColumnOperation> DefinitionID { get; set; }
        public OperationBuilder<AddColumnOperation> Description { get; set; }
        public OperationBuilder<AddColumnOperation> SiteID { get; set; }
        public OperationBuilder<AddColumnOperation> SystemList { get; set; }
    }
}
