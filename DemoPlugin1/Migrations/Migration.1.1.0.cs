﻿using Mystique.Core.DomainModel;
using Mystique.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace DemoPlugin1.Migrations
{
    public class Migration_2_0_0 : BaseMigration
    {
        private static Mystique.Core.DomainModel.Version _version = new Mystique.Core.DomainModel.Version("1.1.0");
        private static string _upScripts = @"CREATE TABLE [dbo].[Test2](
                        TestId[uniqueidentifier] NOT NULL,
                    );";
        private static string _downScripts = @"DROP TABLE [dbo].[Test2]";

        public Migration_2_0_0(DbHelper dbHelper) : base(dbHelper, _version)
        {

        }

        public override void MigrationDown(Guid pluginId)
        {
            SQL(_downScripts);

            base.RemoveMigrationScripts(pluginId);
        }

        public override void MigrationUp(Guid pluginId)
        {
            SQL(_upScripts);

            base.WriteMigrationScripts(pluginId, _upScripts, _downScripts);
        }
    }
}
