using System.Globalization;
using System.Text.RegularExpressions;
namespace IterationPlanner;
public record Sprint(string Project,string Path,DateOnly Start,DateOnly Finish);
public record Proposal(Sprint Sprint,string Status,string Detail);
public record Window(DateOnly Requested,DateOnly Effective,int Duration,int Number,IReadOnlyList<Proposal> Items,int Gap,bool NeedsAcceptance);
public static class Planner
{
 public static DateOnly Parse(string value) { var p=value.Split('/').Select(int.Parse).ToArray(); return DateOnly.FromDateTime(new PersianCalendar().ToDateTime(p[0],p[1],p[2],0,0,0,0)); }
 public static string Persian(DateOnly d) {var c=new PersianCalendar();var t=d.ToDateTime(TimeOnly.MinValue);return $"{c.GetYear(t):0000}/{c.GetMonth(t):00}/{c.GetDayOfMonth(t):00}";}
 public static string MonthName(DateOnly date)
 {
 string[] names=["فروردین","اردیبهشت","خرداد","تیر","مرداد","شهریور","مهر","آبان","آذر","دی","بهمن","اسفند"];
 return names[new PersianCalendar().GetMonth(date.ToDateTime(TimeOnly.MinValue))-1];
 }
 public static string WeekOfMonth(DateOnly date)
 {
 string[] names=["اول","دوم","سوم","چهارم","پنجم"];
 return names[(new PersianCalendar().GetDayOfMonth(date.ToDateTime(TimeOnly.MinValue))-1)/7];
 }
 public static string RenderName(string pattern,DateOnly start,int number,bool english=false,bool gregorian=false)
 {
 Calendar calendar=gregorian?new GregorianCalendar():new PersianCalendar();var date=start.ToDateTime(TimeOnly.MinValue);
 return pattern.Replace("{n}",number.ToString(CultureInfo.InvariantCulture))
 .Replace("{month}",NamingMonth(start,english,gregorian)).Replace("{week}",english?new[]{"First","Second","Third","Fourth","Fifth"}[(calendar.GetDayOfMonth(date)-1)/7]:WeekOfMonth(start))
 .Replace("{year}",calendar.GetYear(date).ToString(CultureInfo.InvariantCulture));
 }
 public static string NamingMonth(DateOnly start,bool english,bool gregorian)
 {
 if(gregorian)return start.ToString("MMMM",CultureInfo.GetCultureInfo(english?"en-US":"fa-IR"));
 if(!english)return MonthName(start);
 return new[]{"Farvardin","Ordibehesht","Khordad","Tir","Mordad","Shahrivar","Mehr","Aban","Azar","Dey","Bahman","Esfand"}[new PersianCalendar().GetMonth(start.ToDateTime(TimeOnly.MinValue))-1];
 }
 public static string ParentPath(string root,DateOnly start,string hierarchy,bool english=false,bool gregorian=false)
 {
 Calendar c=gregorian?new GregorianCalendar():new PersianCalendar();var date=start.ToDateTime(TimeOnly.MinValue);
 var parts=new List<string>();if(!string.IsNullOrWhiteSpace(root))parts.Add(root.Trim('/'));
 if(hierarchy!="Custom"){
 parts.Add(c.GetYear(date).ToString(CultureInfo.InvariantCulture));
 if(hierarchy is "YearSeason" or "YearSeasonMonth")parts.Add("Q"+((c.GetMonth(date)-1)/3+1));
 if(hierarchy is "YearMonth" or "YearSeasonMonth")parts.Add(NamingMonth(start,english,gregorian));
 }
 return string.Join("/",parts);
 }
 public static DateOnly Align(DateOnly d,DayOfWeek weekday)=>d.AddDays(((int)weekday-(int)d.DayOfWeek+7)%7);
 public static Sprint[] Relevant(IEnumerable<Sprint> all,string project,string branch)=>all.Where(s=>s.Project==project && (string.IsNullOrEmpty(branch)||s.Path.StartsWith(branch+"/",StringComparison.Ordinal))).ToArray();
 public static DateOnly Continue(IEnumerable<Sprint> s) { var items=s.ToArray(); if(items.Length==0)throw new ArgumentException("اسپرینت موجودی برای ادامه یافت نشد؛ تاریخ شروع را دستی وارد کنید."); return items.Max(x=>x.Finish).AddDays(1); }
 public static int Suggest(IEnumerable<Sprint> s)=>s.Select(x=>Regex.Match(x.Path,@"(\d+)$")).Where(m=>m.Success).Select(m=>int.Parse(m.Value)).DefaultIfEmpty(0).Max()+1;
 public static Window Generate(string project,string branch,DateOnly requested,DayOfWeek weekday,int duration,int count,int number,string pattern,IEnumerable<Sprint> existing,bool accepted=false,DateOnly? through=null,string hierarchy="Custom",bool english=false,bool gregorian=false)
 {
 if(duration<1||duration>366||count<1||count>1000||number<1)throw new ArgumentException("مدت، تعداد یا شماره نامعتبر است");
 if(!pattern.Contains("{n}")||pattern.Replace("{n}","").Replace("{month}","").Replace("{week}","").Replace("{year}","").IndexOfAny(['/', '\\', ':','*','?','"','<','>','|','{','}'])>=0)throw new ArgumentException("الگوی نام باید شامل {n} و فاقد نویسه غیرمجاز باشد");
 var old=Relevant(existing,project,branch);var start=Align(requested,weekday);var first=start;var items=new List<Proposal>();
 for(int i=0;i<count;i++) {if(through.HasValue&&start>through.Value)break;var finish=start.AddDays(duration-1);var name=RenderName(pattern,start,number+i,english,gregorian);var parent=ParentPath(branch,start,hierarchy,english,gregorian);var s=new Sprint(project,string.IsNullOrEmpty(parent)?name:parent+"/"+name,start,finish);var same=old.FirstOrDefault(x=>x.Path==s.Path);var overlaps=old.Where(x=>x.Start<=finish&&x.Finish>=start).ToArray();var status="Create";var detail="";
 if(same==s)status="Already exists / Skip";
 else if(same!=null||overlaps.Length>0){status="Blocked";detail=string.Join(", ",overlaps.Select(x=>x.Path));if(same!=null)detail+=" مسیر موجود: "+same.Path;}
 if(start!=requested&&!accepted&&i==0){status="Blocked";detail+=" پذیرش تاریخ تنظیم‌شده لازم است";}
 items.Add(new(s,status,detail));start=finish.AddDays(1);}
 var previous=old.Where(x=>x.Finish<first).OrderBy(x=>x.Finish).LastOrDefault();return new(requested,first,duration,number,items,previous==null?0:first.DayNumber-previous.Finish.DayNumber-1,first!=requested&&!accepted);
 }
}






