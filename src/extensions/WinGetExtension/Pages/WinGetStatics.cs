﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Management.Deployment;
using WindowsPackageManager.Interop;

namespace WinGetExtension.Pages;

internal static class WinGetStatics
{
    public static WindowsPackageManagerStandardFactory WinGetFactory { get; private set; }

    public static PackageManager Manager { get; private set; }

    public static IReadOnlyList<PackageCatalogReference> AvailableCatalogs { get; private set; }

    public static IEnumerable<PackageCatalog> Connections { get; private set; }

    static WinGetStatics()
    {
        WinGetFactory = new WindowsPackageManagerStandardFactory();

        // Create Package Manager and get available catalogs
        Manager = WinGetFactory.CreatePackageManager();

        // AvailableCatalogs = Manager.GetPackageCatalogs();
        AvailableCatalogs = [
            Manager.GetPredefinedPackageCatalog(PredefinedPackageCatalog.OpenWindowsCatalog),
            Manager.GetPredefinedPackageCatalog(PredefinedPackageCatalog.MicrosoftStore),
        ];

        Connections = AvailableCatalogs
            .ToArray()
            .Select(reference => reference.Connect().PackageCatalog);
    }
}
