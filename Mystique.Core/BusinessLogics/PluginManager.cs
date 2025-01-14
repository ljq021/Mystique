﻿using Microsoft.Extensions.Options;
using Mystique.Core.Contracts;
using Mystique.Core.DomainModel;
using Mystique.Core.Models;
using Mystique.Core.Repositories;
using Mystique.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Version = Mystique.Core.DomainModel.Version;

namespace Mystique.Core.BusinessLogics
{
    public class PluginManager : IPluginManager
    {
        private readonly IUnitOfWork _unitOfWork = null;
        private string _connectionString = null;
        private IMvcModuleSetup _mvcModuleSetup = null;

        public PluginManager(IUnitOfWork unitOfWork, IOptions<ConnectionStringSetting> connectionStringSettingAccessor, IMvcModuleSetup mvcModuleSetup)
        {
            _unitOfWork = unitOfWork;
            _connectionString = connectionStringSettingAccessor.Value.ConnectionString;
            _mvcModuleSetup = mvcModuleSetup;
        }

        public List<PluginListItemViewModel> GetAllPlugins()
        {
            return _unitOfWork.PluginRepository.GetAllPlugins();
        }

        public List<PluginListItemViewModel> GetAllEnabledPlugins()
        {
            return _unitOfWork.PluginRepository.GetAllEnabledPlugins();
        }

        public PluginViewModel GetPlugin(Guid pluginId)
        {
            return _unitOfWork.PluginRepository.GetPlugin(pluginId);
        }

        public void EnablePlugin(Guid pluginId)
        {
            var module = _unitOfWork.PluginRepository.GetPlugin(pluginId);
            _unitOfWork.PluginRepository.SetPluginStatus(pluginId, true);

            _mvcModuleSetup.EnableModule(module.Name);
        }

        public void DeletePlugin(Guid pluginId)
        {
            var plugin = _unitOfWork.PluginRepository.GetPlugin(pluginId);

            if (plugin.IsEnable)
            {
                DisablePlugin(pluginId);
            }

            _unitOfWork.PluginRepository.RunDownMigrations(pluginId);
            _unitOfWork.PluginRepository.DeletePlugin(pluginId);
            _unitOfWork.Commit();

            _mvcModuleSetup.DeleteModule(plugin.Name);
        }

        public void DisablePlugin(Guid pluginId)
        {
            var module = _unitOfWork.PluginRepository.GetPlugin(pluginId);
            _unitOfWork.PluginRepository.SetPluginStatus(pluginId, false);
            _mvcModuleSetup.DisableModule(module.Name);
        }

        public void AddPlugins(PluginPackage pluginPackage)
        {
            var existedPlugin = _unitOfWork.PluginRepository.GetPlugin(pluginPackage.Configuration.Name);

            if (existedPlugin == null)
            {
                InitializePlugin(pluginPackage);
            }
            else if (new Version(pluginPackage.Configuration.Version) > new Version(existedPlugin.Version))
            {
                UpgradePlugin(pluginPackage, existedPlugin);
            }
            else if (new Version(pluginPackage.Configuration.Version) == new Version(existedPlugin.Version))
            {
                throw new Exception("The package version is same as the current plugin version.");
            }
            else
            {
                DegradePlugin(pluginPackage, existedPlugin);
            }
        }

        private void InitializePlugin(PluginPackage pluginPackage)
        {
            var plugin = new DTOs.AddPluginDTO
            {
                Name = pluginPackage.Configuration.Name,
                DisplayName = pluginPackage.Configuration.DisplayName,
                PluginId = Guid.NewGuid(),
                UniqueKey = pluginPackage.Configuration.UniqueKey,
                Version = pluginPackage.Configuration.Version
            };

            _unitOfWork.PluginRepository.AddPlugin(plugin);
            _unitOfWork.Commit();

            var versions = pluginPackage.GetAllMigrations(_connectionString);

            foreach (var version in versions)
            {
                version.MigrationUp(plugin.PluginId);
            }

            pluginPackage.SetupFolder();
        }

        public void UpgradePlugin(PluginPackage pluginPackage, PluginViewModel oldPlugin)
        {
            _unitOfWork.PluginRepository.UpdatePluginVersion(oldPlugin.PluginId, pluginPackage.Configuration.Version);
            _unitOfWork.Commit();

            var migrations = pluginPackage.GetAllMigrations(_connectionString);

            var pendingMigrations = migrations.Where(p => p.Version > oldPlugin.Version);

            foreach (var migration in pendingMigrations)
            {
                migration.MigrationUp(oldPlugin.PluginId);
            }

            pluginPackage.SetupFolder();
        }

        public void DegradePlugin(PluginPackage pluginPackage, PluginViewModel oldPlugin)
        {
            _unitOfWork.PluginRepository.UpdatePluginVersion(oldPlugin.PluginId, pluginPackage.Configuration.Version);
            _unitOfWork.Commit();
        }
    }
}
