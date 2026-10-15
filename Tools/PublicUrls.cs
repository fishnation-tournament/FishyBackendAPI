namespace FishyAPI.Tools;

// Public base URLs used in OAuth redirects. Overridden per environment so the dev server redirects to itself.
public static class PublicUrls
{
    public static readonly string Api = (Environment.GetEnvironmentVariable("PUBLIC_API_URL") ?? "https://api.fishnation.xyz").TrimEnd('/');
    public static readonly string Site = (Environment.GetEnvironmentVariable("PUBLIC_SITE_URL") ?? "https://fishnation.xyz").TrimEnd('/');
}
