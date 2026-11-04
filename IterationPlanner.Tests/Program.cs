using IterationPlanner;
int passed=0;
void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);passed++;}
var sat=new DateOnly(2026,11,7);
Check(Planner.Align(sat,DayOfWeek.Saturday)==sat,"Matching weekday");
Check(Planner.Align(sat.AddDays(1),DayOfWeek.Saturday)==sat.AddDays(7),"Advance forward");
var old=new[]{new Sprint("A","1405/Sprint 1",sat,sat.AddDays(13)),new Sprint("B","1405/Sprint 9",sat,sat.AddDays(27))};
Check(Planner.Continue(Planner.Relevant(old,"A","1405"))==sat.AddDays(14),"Continuation A");
Check(Planner.Continue(Planner.Relevant(old,"B","1405"))==sat.AddDays(28),"Independent continuation B");
var w=Planner.Generate("A","1405",sat.AddDays(14),DayOfWeek.Saturday,14,2,2,"Sprint {n}",old);
Check(w.Gap==0&&w.Items[0].Sprint.Finish==sat.AddDays(27),"Inclusive continuous dates");
Check(Planner.Generate("A","1405",sat.AddDays(21),DayOfWeek.Saturday,14,1,2,"Sprint {n}",old).Gap==7,"Gap");
Check(Planner.Generate("A","1405",sat.AddDays(1),DayOfWeek.Saturday,14,1,2,"Sprint {n}",old).NeedsAcceptance,"Adjustment requires acceptance");
var future=old.Append(new Sprint("A","1405/Future",sat.AddDays(21),sat.AddDays(34))).ToArray();
Check(Planner.Generate("A","1405",sat.AddDays(14),DayOfWeek.Saturday,14,1,2,"Sprint {n}",future).Items[0].Status=="Blocked","Future overlap blocked");
Check(old[0].Finish==sat.AddDays(13)&&old[0].Path=="1405/Sprint 1","Preserve previous sprints");
Check(Planner.Parse(Planner.Persian(sat))==sat,"Persian roundtrip");
Check(Planner.Generate("A","1405",sat,DayOfWeek.Saturday,14,1,1,"Sprint {n}",old).Items[0].Status.Contains("Skip"),"Idempotency");
Check(Planner.MonthName(Planner.Parse("1405/01/01"))=="فروردین","First Persian month");
Check(Planner.MonthName(Planner.Parse("1405/12/01"))=="اسفند","Last Persian month");
var months=Planner.Generate("C","",Planner.Parse("1405/01/29"),Planner.Parse("1405/01/29").DayOfWeek,14,2,1,"Sprint {month} {n}",[]);
Check(months.Items[0].Sprint.Path=="Sprint فروردین 1"&&months.Items[1].Sprint.Path=="Sprint اردیبهشت 2","Names follow sprint start month across boundary");
Check(Planner.RenderName("Sprint {n} - هفته {week} {month}",Planner.Parse("1405/08/08"),2)=="Sprint 2 - هفته دوم آبان","English first naming");
Check(Planner.RenderName("هفته {week} {month} - Sprint {n}",Planner.Parse("1405/08/14"),2)=="هفته دوم آبان - Sprint 2","Persian first naming");
Check(Planner.WeekOfMonth(Planner.Parse("1405/08/07"))=="اول"&&Planner.WeekOfMonth(Planner.Parse("1405/08/15"))=="سوم"&&Planner.WeekOfMonth(Planner.Parse("1405/08/29"))=="پنجم","Week bucket boundaries");
Check(Planner.RenderName("{year} {month} {n}",Planner.Parse("1405/01/01"),1)=="1405 فروردین 1","Year naming token");
Check(Planner.ParentPath("",Planner.Parse("1405/01/01"),"YearSeasonMonth")=="1405/Q1/فروردین","Full Persian hierarchy");
Check(Planner.ParentPath("",Planner.Parse("1405/08/01"),"YearSeason")=="1405/Q3","Persian quarter");
Check(Planner.ParentPath("",Planner.Parse("1405/12/01"),"YearMonth")=="1405/اسفند","Year month hierarchy");
Check(Planner.ParentPath("",Planner.Parse("1405/01/01"),"Year")=="1405","Flat year hierarchy");
Check(Planner.RenderName("Sprint {n} - Week {week} of {month}",new DateOnly(2026,1,8),2,true,true)=="Sprint 2 - Week Second of January","English Gregorian names");
Check(Planner.ParentPath("",new DateOnly(2026,4,1),"YearSeasonMonth",true,true)=="2026/Q2/April","Gregorian hierarchy");
Check(Planner.ParentPath("",Planner.Parse("1405/01/01"),"YearSeasonMonth",true,false)=="1405/Q1/Farvardin","English Persian hierarchy");
Console.WriteLine($"{passed} checks passed");




