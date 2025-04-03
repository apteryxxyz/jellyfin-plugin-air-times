# Jellyfin Air Times Plugin

Air Times is a Jellyfin plugin that provides accurate, localised series air times using data from TheTVDB.

### Why Use This Plugin?

TheTVDB air times are unstandardised and can be ambiguous due to the way they represent broadcast schedules. The default TheTVDB plugin in Jellyfin does not attempt to localise these air times, which can result in incorrect or misleading information.

The Air Times plugin fixes this by ensuring air times are correctly adjusted for your server’s location, providing more accurate metadata.

For best results, make sure this plugin is higher in the metadata sources list than the default TheTVDB plugin (or any other plugin that might provide air times).

## Installation

You can install this plugin using Jellyfin’s plugin catalog and repositories.

First, open Jellyfin and go to `Dashboard` -> `Catalog`. Click the settings icon to open the plugin repository list, then add a new repository with the following details:

- Repository Name: Air Times
- Repository URL: https://raw.githubusercontent.com/apteryxxyz/jellyfin-plugin-airtimes/main/manifest.json

Save your changes and return to the plugin catalog. Locate the Air Times plugin and install it. Once the installation is complete, restart Jellyfin.

After restarting, you can enable Air Times as a metadata source by editing any Shows library in `Dashboard` -> `Libraries` and ensure it is above the default TheTVDB plugin.