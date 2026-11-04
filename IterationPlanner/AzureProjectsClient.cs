using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
namespace IterationPlanner;
public record AzureProject(string Id,string Name);
public sealed class AzureProjectsClient(HttpClient http,IOptions<AzureDevOpsOptions> options)
{
 public async Task<List<AzureProject>> ListAsync()
 {
 var settings=options.Value;
 if(string.IsNullOrWhiteSpace(settings.CollectionUrl))throw new InvalidOperationException("آدرس مجموعه در appsettings تنظیم نشده است.");
 if(string.IsNullOrWhiteSpace(settings.Pat))throw new InvalidOperationException("توکن PAT در تنظیمات خالی است. ورود مرورگر جایگزین احراز هویت برنامه نیست.");
 using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(settings.TimeoutSeconds));
 var projects=new List<AzureProject>();string? continuation=null;
 do {
 var url=settings.CollectionUrl.TrimEnd('/')+"/_apis/projects?api-version="+Uri.EscapeDataString(settings.ApiVersion);
 if(continuation!=null)url+="&continuationToken="+Uri.EscapeDataString(continuation);
 using var request=new HttpRequestMessage(HttpMethod.Get,url);
 request.Headers.Authorization=new AuthenticationHeaderValue("Basic",Convert.ToBase64String(Encoding.UTF8.GetBytes(":"+settings.Pat)));
 request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
 using var response=await http.SendAsync(request,timeout.Token);
 if(!response.IsSuccessStatusCode)throw new InvalidOperationException((int)response.StatusCode switch {
 401=>"احراز هویت ناموفق است؛ PAT را بررسی کنید.",403=>"دسترسی مشاهده پروژه‌ها کافی نیست.",404=>"آدرس مجموعه یا مسیر API پیدا نشد.",_=>$"درخواست پروژه‌ها ناموفق بود (HTTP {(int)response.StatusCode}). نسخه API و تنظیمات سرور را بررسی کنید."});
 using var json=await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(timeout.Token),cancellationToken:timeout.Token);
 if(!json.RootElement.TryGetProperty("value",out var values)||values.ValueKind!=JsonValueKind.Array)throw new InvalidOperationException("پاسخ سرور فهرست پروژه‌ها نیست.");
 foreach(var item in values.EnumerateArray())projects.Add(new(item.GetProperty("id").GetString()!,item.GetProperty("name").GetString()!));
 var next=response.Headers.TryGetValues("x-ms-continuationtoken",out var tokens)?tokens.FirstOrDefault():null;
 if(next==continuation&&next!=null)throw new InvalidOperationException("پاسخ صفحه‌بندی سرور تکراری است.");
 continuation=string.IsNullOrWhiteSpace(next)?null:next;
 }while(continuation!=null);
 return projects;
 }
}
