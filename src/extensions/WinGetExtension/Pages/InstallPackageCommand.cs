﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;
using Microsoft.Management.Deployment;
using Windows.Foundation;

namespace WinGetExtension.Pages;

public partial class InstallPackageCommand : InvokableCommand
{
    private readonly CatalogPackage _package;

    private readonly StatusMessage _installBanner = new();
    private IAsyncOperationWithProgress<InstallResult, InstallProgress>? _installAction;
    private IAsyncOperationWithProgress<UninstallResult, UninstallProgress>? _unInstallAction;
    private Task? _installTask;

    public bool IsInstalled { get; private set; }

    public InstallPackageCommand(CatalogPackage package, bool isInstalled)
    {
        _package = package;
        IsInstalled = isInstalled;

        Icon = new(isInstalled ? "\uE930" : "\uE896"); // Completed : Download
        Name = isInstalled ? "Uninstall" : "Install";
    }

    public override ICommandResult Invoke()
    {
        // var result = _package.CheckInstalledStatus();
        // if (result.Status == CheckInstalledStatusResultStatus.Ok)
        // {
        //    var isInstalled = _package.InstalledVersion != null;

        // if (isInstalled)
        //    {
        //        _installBanner.State = MessageState.Info;
        //        _installBanner.Message = $"{_package.Name} is already installed";
        //        ExtensionHost.ShowStatus(_installBanner);

        // // TODO Derp, didn't expose HideStatus from API
        //        // _ = Task.Run(() =>
        //        // {
        //        //    Thread.Sleep(2000);
        //        //    ExtensionHost.HideStatus(_installBanner);
        //        // });
        //    }
        //    else
        //    {
        // _ = Task.Run(() =>
        // {
        //    Thread.Sleep(2000);
        //    _installBanner.State = MessageState.Success;
        //    _installBanner.Message = $"Successfully installed {_package.Name}";
        // });
        if (IsInstalled)
        {
            // Uninstall
            _installBanner.State = MessageState.Info;
            _installBanner.Message = $"Uninstalling {_package.Name}...";
            ExtensionHost.ShowStatus(_installBanner);

            var installOptions = WinGetStatics.WinGetFactory.CreateUninstallOptions();
            installOptions.PackageUninstallScope = PackageUninstallScope.Any;
            _unInstallAction = WinGetStatics.Manager.UninstallPackageAsync(_package, installOptions);

            _installTask = Task.Run(async () =>
            {
                try
                {
                    await _unInstallAction.AsTask();
                }
                catch (Exception ex)
                {
                    _installBanner.State = MessageState.Error;
                    _installBanner.Message = ex.Message;
                }
            });
        }
        else
        {
            // Install
            _installBanner.State = MessageState.Info;
            _installBanner.Message = $"Installing {_package.Name}...";
            ExtensionHost.ShowStatus(_installBanner);

            var installOptions = WinGetStatics.WinGetFactory.CreateInstallOptions();
            installOptions.PackageInstallScope = PackageInstallScope.Any;
            _installAction = WinGetStatics.Manager.InstallPackageAsync(_package, installOptions);

            var handler = new AsyncOperationProgressHandler<InstallResult, InstallProgress>(OnInstallProgress);
            _installAction.Progress = handler;

            _installTask = Task.Run(async () =>
            {
                try
                {
                    await _installAction.AsTask();
                    _installBanner.Message = $"Finished install for {_package.Name}";
                    _installBanner.Progress = null;
                    _installBanner.State = MessageState.Success;
                }
                catch (Exception ex)
                {
                    _installBanner.State = MessageState.Error;
                    _installBanner.Progress = null;
                    _installBanner.Message = ex.Message;
                }
            });
        }

        // }
        // }
        return CommandResult.KeepOpen();
    }

    private static string FormatBytes(ulong bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        return bytes >= GB
            ? $"{bytes / (double)GB:F2} GB"
            : bytes >= MB ?
                $"{bytes / (double)MB:F2} MB"
                : bytes >= KB
                    ? $"{bytes / (double)KB:F2} KB"
                    : $"{bytes} bytes";
    }

    private void OnInstallProgress(
        IAsyncOperationWithProgress<InstallResult, InstallProgress> operation,
        InstallProgress progress)
    {
        var downloadText = "Downloading. ";
        switch (progress.State)
        {
            case PackageInstallProgressState.Queued:
                _installBanner.Message = $"Queued {_package.Name} for download...";
                break;
            case PackageInstallProgressState.Downloading:
                downloadText += $"{FormatBytes(progress.BytesDownloaded)} of {FormatBytes(progress.BytesRequired)}";
                _installBanner.Message = downloadText;
                break;
            case PackageInstallProgressState.Installing:
                _installBanner.Message = $"Installing {_package.Name}...";
                _installBanner.Progress = new ProgressState() { IsIndeterminate = true };
                break;
            case PackageInstallProgressState.PostInstall:
                _installBanner.Message = $"Finishing install for {_package.Name}...";
                break;
            case PackageInstallProgressState.Finished:
                _installBanner.Message = "Finished install.";

                // progressBar.IsIndeterminate(false);
                _installBanner.Progress = null;
                _installBanner.State = MessageState.Success;
                break;
            default:
                _installBanner.Message = string.Empty;
                break;
        }
    }
}
