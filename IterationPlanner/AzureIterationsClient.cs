using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
namespace IterationPlanner;
public record IterationNode(string Path,DateOnly? Start,DateOnly? Finish,bool HasChildren);
public sealed class AzureIterationsClient(HttpClient http,IOptions<AzureDevOpsOptions> options)
{
 async Task<JsonElement> Send(string project,string path,HttpMethod method,object? body=null)
 {
 var o=options.Value;
 if(string.IsNullOrWhiteSpace(o.CollectionUrl)||string.IsNullOrWhiteSpace(o.Pat))throw new InvalidOperationException("آدرس مجموعه و PAT را تنظیم کنید.");
 var url=o.CollectionUrl.TrimEnd('/')+"/"+Uri.EscapeDataString(project)+"/_apis/wit/classificationnodes/iterations";
 if(path!="")url+="/"+string.Join("/",path.Split('/').Select(Uri.EscapeDataString));
 url+="?api-version="+Uri.EscapeDataString(o.ApiVersion)+"&$depth=1";
 using var request=new HttpRequestMessage(method,url);
 request.Headers.Authorization=new AuthenticationHeaderValue("Basic",Convert.ToBase64String(Encoding.UTF8.GetBytes(":"+o.Pat)));
 if(body!=null)request.Content=JsonContent.Create(body);
 using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(o.TimeoutSeconds));
 using var response=await http.SendAsync(request,timeout.Token);
 if(!response.IsSuccessStatusCode)throw new InvalidOperationException($"عملیات Azure ناموفق بود (HTTP {(int)response.StatusCode}). دسترسی Work Items، مسیر و نسخه API را بررسی کنید.");
 using var doc=await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(timeout.Token),cancellationToken:timeout.Token);return doc.RootElement.Clone();
 }
 public async Task<List<IterationNode>> Load(string project)
 {
 var result=new List<IterationNode>();
 async Task Walk(string path,JsonElement node){DateOnly? Read(string key){if(node.TryGetProperty("attributes",out var a)&&a.TryGetProperty(key,out var v)&&v.ValueKind==JsonValueKind.String)return DateOnly.ParseExact(v.GetString()![..10],"yyyy-MM-dd");return null;}
 var has=node.TryGetProperty("hasChildren",out var h)&&h.GetBoolean();
 if(path!="")result.Add(new(path,Read("startDate"),Read("finishDate"),has));
 if(!has)return;
 if(!node.TryGetProperty("children",out var children))throw new InvalidOperationException("درخت اسپرینت‌ها ناقص است؛ انتشار متوقف شد.");
 foreach(var child in children.EnumerateArray()){var childPath=(path==""?"":path+"/")+child.GetProperty("name").GetString();await Walk(childPath,await Send(project,childPath,HttpMethod.Get));}}
 await Walk("",await Send(project,"",HttpMethod.Get));return result;
 }
 public static Sprint[] Leaves(string project,IEnumerable<IterationNode> nodes)=>nodes.Where(n=>!n.HasChildren&&n.Start.HasValue&&n.Finish.HasValue).Select(n=>new Sprint(project,n.Path,n.Start!.Value,n.Finish!.Value)).ToArray();
 public async Task<List<string>> Publish(string project,Window window,List<IterationNode> snapshot)
 {
 var report=new List<string>();var current=await Load(project);
 if(!current.OrderBy(x=>x.Path).SequenceEqual(snapshot.OrderBy(x=>x.Path)))throw new InvalidOperationException("وضعیت Azure از زمان پیش‌نمایش تغییر کرده؛ دوباره اسپرینت‌ها را بارگذاری و بررسی کنید.");
 if(window.Items.Any(x=>x.Status=="Blocked")||window.NeedsAcceptance)throw new InvalidOperationException("تعارض‌های پیش‌نمایش را رفع کنید.");
 foreach(var item in window.Items){var s=item.Sprint;if(s.Project!=project)throw new InvalidOperationException("پروژه پیش‌نمایش تغییر کرده است.");
 foreach(var parent in current.Where(n=>s.Path.StartsWith(n.Path+"/",StringComparison.Ordinal))){if((parent.Start.HasValue&&s.Start<parent.Start)||(parent.Finish.HasValue&&s.Finish>parent.Finish))throw new InvalidOperationException("تعارض تاریخ والد: "+parent.Path);}
 if(current.Any(n=>n.Path==s.Path&&(n.Start!=s.Start||n.Finish!=s.Finish||n.HasChildren)))throw new InvalidOperationException("مسیر موجود متفاوت است: "+s.Path);
 if(Leaves(project,current).Any(x=>x.Path!=s.Path&&x.Start<=s.Finish&&x.Finish>=s.Start))throw new InvalidOperationException("هم‌پوشانی با اسپرینت موجود: "+s.Path);
 }
 foreach(var item in window.Items){var s=item.Sprint;
 try{
 current=await Load(project);
  var found=current.FirstOrDefault(n=>n.Path==s.Path);
 if(found!=null){if(found.Start!=s.Start||found.Finish!=s.Finish||found.HasChildren)throw new InvalidOperationException("مسیر تغییر کرده است.");report.Add("Skip: "+s.Path);continue;}
 if(Leaves(project,current).Any(x=>x.Start<=s.Finish&&x.Finish>=s.Start))throw new InvalidOperationException("هم‌پوشانی جدید شناسایی شد.");
 foreach(var ancestor in current.Where(n=>s.Path.StartsWith(n.Path+"/",StringComparison.Ordinal)))if((ancestor.Start.HasValue&&s.Start<ancestor.Start)||(ancestor.Finish.HasValue&&s.Finish>ancestor.Finish))throw new InvalidOperationException("تعارض تاریخ والد.");
 var parts=s.Path.Split('/');string parent="";
 for(int i=0;i<parts.Length;i++){var path=(parent==""?"":parent+"/")+parts[i];if(!current.Any(n=>n.Path==path)){
 object body=i==parts.Length-1?new {name=parts[i],attributes=new {startDate=s.Start.ToString("yyyy-MM-dd")+"T00:00:00Z",finishDate=s.Finish.ToString("yyyy-MM-dd")+"T00:00:00Z"}}:new {name=parts[i]};
 await Send(project,parent,HttpMethod.Post,body);report.Add("Created: "+path);
 }parent=path;}
 }catch{report.Add("متوقف شد؛ نتیجه آخرین عملیات ممکن است نامشخص باشد. دوباره بارگذاری کنید؛ تغییرات قبلی باقی می‌مانند: "+s.Path);break;}
 }
 return report;
 }
}

