# OuchR Bot repository
> [General repo](https://github.com/RTUITLab/OuchR)

## Environment variables

### **Required**
* `VkBotOptions__GroupId`: VK group id ([171158291 for RTUITLab for example](https://vk.com/dev/groups.getById?params[group_ids]=rtuitlab&params[v]=5.21))
* `VkBotOptions__GroupAccessToken`: VK group Access Token ([docs](https://vk.com/dev/bizmessages_doc))
* `CalendarOptions__GoogleCalendarUrl`: URL to ICS calendar to sync events ([docs](https://support.google.com/calendar/answer/37648?hl=en#zippy=%2Cget-your-calendar-view-only))

### *Optional*
* `USE_MOCK_PROFILE_PARSER_SERVICE`: Use mock profile parser with hardcoded data (default - `false`)
* `DUMP_JSON_DATABASE`: Periodically dump all data uses dump endpoint (`/api/Debug/exportDb`) (default - `true`)

For local development you can create `/API/appsettings.Local.json` file and override variables. [JSON environment docs](https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration-providers#json-configuration-provider).
```json
{
  "CalendarOptions": {
    "GoogleCalendarUrl": "some ICS url"
  },
  "VkBotOptions": {
    "GroupAccessToken": "access token",
    "GroupId": 171158291
  }
}
```

## Development

### Requirements
* [.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0)

### Run locally
```bash
cd API
dotnet run
```

### Build docker image
```bash
docker build -t image_name .
```