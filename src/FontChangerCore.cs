using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Reflection;
using System.Diagnostics;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace RepoFonts {
public class FontInfo {
    public byte[] Data;
    public string Name;
    public int Units;
    public double Asc, Desc, Cap, Mean, Underline, Thickness;
    static int U16(byte[] b,int p) { if(p<0 || p+2>b.Length) throw new Exception("Не удалось прочитать шрифт."); return (b[p]<<8)|b[p+1]; }
    static int S16(byte[] b,int p) { return (short)U16(b,p); }
    static int U32(byte[] b,int p) { if(p<0 || p+4>b.Length) throw new Exception("Не удалось прочитать шрифт."); uint n=((uint)b[p]<<24)|((uint)b[p+1]<<16)|((uint)b[p+2]<<8)|b[p+3]; if(n>int.MaxValue) throw new Exception("Не удалось прочитать шрифт."); return (int)n; }
    public static FontInfo Read(byte[] b) {
        if(b==null || b.Length<32 || b.Length>64000000) throw new Exception("Выбери файл шрифта TTF или OTF.");
        string signature=Encoding.ASCII.GetString(b,0,4);
        if(signature!="OTTO" && !(b[0]==0 && b[1]==1 && b[2]==0 && b[3]==0) && signature!="true") throw new Exception("Этот формат шрифта пока не поддерживается. Нужен TTF или OTF.");
        var tables=new Dictionary<string,int>(); var lengths=new Dictionary<string,int>();
        int count=U16(b,4); if(count<1 || count>512 || 12+count*16>b.Length) throw new Exception("Файл шрифта повреждён.");
        for(int i=0;i<count;i++) { int p=12+i*16; string key=Encoding.ASCII.GetString(b,p,4); int at=U32(b,p+8),len=U32(b,p+12); if(at>b.Length-len) throw new Exception("Файл шрифта повреждён."); tables[key]=at; lengths[key]=len; }
        foreach(string t in new string[]{"head","hhea","maxp","cmap","name"}) if(!tables.ContainsKey(t)) throw new Exception("В шрифте не хватает необходимых данных.");
        if(lengths["head"]<54 || lengths["hhea"]<10) throw new Exception("Файл шрифта повреждён.");
        var f=new FontInfo(); f.Data=b; f.Units=U16(b,tables["head"]+18);
        if(f.Units<16 || f.Units>16384) throw new Exception("Не удалось определить размер шрифта.");
        f.Asc=S16(b,tables["hhea"]+4); f.Desc=S16(b,tables["hhea"]+6); f.Cap=f.Units*0.7; f.Mean=f.Units*0.5;
        if(tables.ContainsKey("OS/2") && lengths["OS/2"]>=90 && U16(b,tables["OS/2"])>=2) { int mean=S16(b,tables["OS/2"]+86),cap=S16(b,tables["OS/2"]+88); if(cap>0)f.Cap=cap; if(mean>0)f.Mean=mean; }
        f.Underline=-f.Units*0.1; f.Thickness=f.Units*0.05;
        if(tables.ContainsKey("post") && lengths["post"]>=12) { f.Underline=S16(b,tables["post"]+8); f.Thickness=S16(b,tables["post"]+10); }
        f.Name="Ваш шрифт";
        int ns=tables["name"],ncount=U16(b,ns+2),strings=ns+U16(b,ns+4); string full=null,family=null,subfamily=null; int fullRank=-1,familyRank=-1,subRank=-1;
        for(int i=0;i<ncount;i++) {
            int p=ns+6+i*12; if(p+12>ns+lengths["name"])break;
            int platform=U16(b,p),language=U16(b,p+4),nid=U16(b,p+6),len=U16(b,p+8),at=strings+U16(b,p+10);
            if((nid!=1 && nid!=2 && nid!=4) || at<0 || at>b.Length-len)continue;
            string value=(platform==3 || platform==0)?Encoding.BigEndianUnicode.GetString(b,at,len):Encoding.ASCII.GetString(b,at,len);
            value=value.Trim();if(value.Length==0)continue;
            int rank=platform==3 && language==0x409?4:platform==0?3:platform==3?2:1;
            if(nid==4 && rank>fullRank){full=value;fullRank=rank;}else if(nid==1 && rank>familyRank){family=value;familyRank=rank;}else if(nid==2 && rank>subRank){subfamily=value;subRank=rank;}
        }
        if(!string.IsNullOrEmpty(full))f.Name=full;else if(!string.IsNullOrEmpty(family))f.Name=family+(string.IsNullOrEmpty(subfamily)?"":" "+subfamily);
        return f;
    }
}
public class Options { public double Spacing=1.0; public bool SeparateBold=true, BodyMatchesHeading, HeadingRegularPreset; public string RegularPath, BoldPath; }
public class Engine {
    readonly Dictionary<string,object> spec;
    public string SourceHash { get { return (string)spec["sourceHash"]; } }
    public string BuildId { get { return (string)spec["build"]; } }
    static Dictionary<string,object> D(object o) { return (Dictionary<string,object>)o; }
    static IEnumerable A(object o) { return (IEnumerable)o; }
    static int I(object o) { return Convert.ToInt32(o); }
    static byte[] Bytes(object o) { var list=new List<byte>(); foreach(object v in A(o))list.Add(Convert.ToByte(v)); return list.ToArray(); }
    static JavaScriptSerializer Json() { var j=new JavaScriptSerializer();j.MaxJsonLength=int.MaxValue;j.RecursionLimit=200;return j; }
    public Engine() {
        using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("RepoFontTemplate"))
        using(var gz=new GZipStream(stream,CompressionMode.Decompress))
        using(var reader=new StreamReader(gz,Encoding.UTF8)) spec=D(Json().DeserializeObject(reader.ReadToEnd()));
    }
    public byte[] DefaultFont(bool bold) {
        foreach(object row in A(spec["fonts"])) {var d=D(row);if(((string)d["style"]=="Bold")==bold)return Bytes(d["fontBytes"]);}
        throw new Exception("Font preset is missing.");
    }
    public static string Hash(string path) { using(var s=File.OpenRead(path))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(s)).Replace("-","").ToLowerInvariant(); }
    static void F(byte[] b,int at,double n) { Buffer.BlockCopy(BitConverter.GetBytes((float)n),0,b,at,4); }
    static void N(byte[] b,int at,int n) { Buffer.BlockCopy(BitConverter.GetBytes(n),0,b,at,4); }
    static void L(byte[] b,int at,long n) { Buffer.BlockCopy(BitConverter.GetBytes(n),0,b,at,8); }
    static byte[] FontObject(Dictionary<string,object> description,byte[] font) {
        using(var s=new MemoryStream())using(var w=new BinaryWriter(s)) { w.Write(Bytes(description["fontPrefix"]));w.Write(font.Length);w.Write(font);while(s.Position%4!=0)w.Write((byte)0);w.Write(Bytes(description["fontSuffix"]));return s.ToArray(); }
    }
    public string Build(string original,string output,Options options) {
        if(Hash(original)!=SourceHash)throw new Exception("Эта версия игры пока не поддерживается.");
        if(options.Spacing<0 || options.Spacing>10)throw new Exception("Проверь межбуквенный интервал.");
        byte[] source=File.ReadAllBytes(original),header=Bytes(spec["header"]);
        var changed=new Dictionary<int,byte[]>();
        foreach(object row in A(spec["objects"])) {var d=D(row);if(d.ContainsKey("data"))changed[I(d["id"])]=Bytes(d["data"]);}
        foreach(object row in A(spec["fonts"])) {
            var d=D(row); bool bold=(string)d["style"]=="Bold";
            byte[] raw;
            string path=bold?options.BoldPath:options.RegularPath;
            if(options.BodyMatchesHeading)raw=!string.IsNullOrEmpty(options.BoldPath)?File.ReadAllBytes(options.BoldPath):DefaultFont(!options.HeadingRegularPreset);
            else if(bold && string.IsNullOrEmpty(path) && options.HeadingRegularPreset)raw=DefaultFont(false);
            else if(!string.IsNullOrEmpty(path))raw=File.ReadAllBytes(path);
            else if(bold && !options.SeparateBold && !string.IsNullOrEmpty(options.RegularPath))raw=File.ReadAllBytes(options.RegularPath);
            else raw=Bytes(d["fontBytes"]);
            FontInfo font=FontInfo.Read(raw); int assetId=I(d["assetId"]);
            changed[I(d["fontId"])]=FontObject(d,raw);
            byte[] target=changed[assetId];var face=D(d["face"]);var fields=D(d["fields"]);
            double fac=90.0/font.Units,cap=font.Cap*fac,scale=56.0/cap;
            var metrics=new Dictionary<string,double>{{"m_PointSize",90},{"m_Scale",scale},{"m_LineHeight",128.97000122070312/scale},{"m_AscentLine",font.Asc*fac},{"m_CapLine",cap},{"m_MeanLine",font.Mean*fac},{"m_Baseline",0},{"m_DescentLine",font.Desc*fac},{"m_SuperscriptOffset",font.Asc*fac},{"m_SubscriptOffset",font.Desc*fac},{"m_UnderlineOffset",font.Underline*fac},{"m_UnderlineThickness",font.Thickness*fac},{"m_StrikethroughOffset",font.Mean*fac*0.4},{"m_StrikethroughThickness",font.Thickness*fac}};
            foreach(var metric in metrics)F(target,I(face[metric.Key]),metric.Value);
            N(target,I(face["m_UnitsPerEM"]),font.Units);
            F(target,I(fields["normalStyle"]),0);
            F(target,I(fields["boldStyle"]),0);
            F(target,I(fields["normalSpacingOffset"]),90.0*options.Spacing/100.0);
            F(target,I(fields["boldSpacing"]),0);
            foreach(object slot in A(d["weightSlots"])) L(target,I(slot),!bold && options.SeparateBold && !options.BodyMatchesHeading?19116:0);
        }
        foreach(object row in A(spec["materials"])) {
            var d=D(row);var slots=D(d["slots"]);byte[] target=changed[I(d["id"])];
            F(target,I(slots["_WeightNormal"]),0);
            F(target,I(slots["_WeightBold"]),0);
        }
        using(var stream=new FileStream(output,FileMode.Create,FileAccess.Write,FileShare.None)) {
            stream.Write(header,0,header.Length);
            foreach(object row in A(spec["objects"])) {
                var d=D(row);int id=I(d["id"]),slot=I(d["slot"]);byte[] custom;
                int length=changed.TryGetValue(id,out custom)?custom.Length:I(d["sourceLength"]);
                L(header,slot,stream.Position-I(spec["dataOffset"]));N(header,slot+8,length);
                if(custom!=null)stream.Write(custom,0,custom.Length);else stream.Write(source,I(d["sourceOffset"]),length);
                while(stream.Position%8!=0)stream.WriteByte(0);
            }
            byte[] size=BitConverter.GetBytes(stream.Length);Array.Reverse(size);Buffer.BlockCopy(size,0,header,24,8);
            stream.Position=0;stream.Write(header,0,header.Length);
        }
        return Hash(output);
    }
    public static string FindGame() {
        var paths=new List<string>();
        paths.Add(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
        var parent=Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));if(parent!=null)paths.Add(parent.FullName);
        var roots=new List<string>();using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))if(key!=null) {var val=key.GetValue("SteamPath") as string;if(!string.IsNullOrEmpty(val))roots.Add(val);}
        roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Steam"));
        foreach(string root in roots) {
            paths.Add(Path.Combine(root,@"steamapps\common\REPO"));string libraries=Path.Combine(root,@"steamapps\libraryfolders.vdf");
            if(File.Exists(libraries))foreach(Match m in Regex.Matches(File.ReadAllText(libraries),"\"path\"\\s+\"([^\"]+)\""))paths.Add(Path.Combine(m.Groups[1].Value.Replace(@"\\",@"\"),@"steamapps\common\REPO"));
        }
        foreach(string path in paths)if(File.Exists(Path.Combine(path,"REPO.exe")) && File.Exists(Path.Combine(path,@"REPO_Data\sharedassets0.assets")))return Path.GetFullPath(path);
        return "";
    }
    public static void EnsureClosed(string game) {
        string exe=Path.GetFullPath(Path.Combine(game,"REPO.exe"));
        foreach(var process in Process.GetProcessesByName("REPO"))using(process) {
            string path;try {path=process.MainModule.FileName;}catch {throw new Exception("Закрой игру и попробуй ещё раз.");}
            if(string.Equals(path,exe,StringComparison.OrdinalIgnoreCase))throw new Exception("Закрой игру и попробуй ещё раз.");
        }
    }
    public string ResourcePath(string game) { return Path.Combine(Path.GetFullPath(game),@"REPO_Data\sharedassets0.assets"); }
    public string BackupPath(string game) { return Path.Combine(Path.GetFullPath(game),"FontPatchBackup-"+BuildId,"sharedassets0.assets.original"); }
    string StatePath(string game) {return Path.Combine(Path.GetDirectoryName(BackupPath(game)),"font-changer-state.json");}
    bool Known(string hash,string game) {
        if(hash==SourceHash)return true;
        foreach(object val in A(spec["knownHashes"]))if(hash==(string)val)return true;
        string state=StatePath(game);
        if(File.Exists(state)) {var d=D(Json().DeserializeObject(File.ReadAllText(state)));if(d.ContainsKey("sourceHash") && (string)d["sourceHash"]==SourceHash)foreach(string key in new string[]{"installedHash","previousHash"})if(d.ContainsKey(key) && hash==(string)d[key])return true;}
        return false;
    }
    public string Install(string game,Options options) {
        EnsureClosed(game);string resource=ResourcePath(game),backup=BackupPath(game),before=Hash(resource);
        if(!Known(before,game))throw new Exception("Эта версия игры пока не поддерживается. Файлы оставлены как есть.");
        Directory.CreateDirectory(Path.GetDirectoryName(backup));
        if(!File.Exists(backup)) {if(before!=SourceHash)throw new Exception("Не найдена исходная копия игры для замены шрифта.");File.Copy(resource,backup);}
        if(Hash(backup)!=SourceHash)throw new Exception("Не удалось проверить исходную копию игры.");
        string temporary=resource+".fontpatch-new",newHash=Build(backup,temporary,options);
        EnsureClosed(game);if(Hash(resource)!=before)throw new Exception("Файл игры изменился. Попробуй ещё раз.");
        var state=new Dictionary<string,object>{{"sourceHash",SourceHash},{"installedHash",newHash},{"previousHash",before}};
        File.WriteAllText(StatePath(game),Json().Serialize(state),Encoding.UTF8);
        File.Replace(temporary,resource,null);if(Hash(resource)!=newHash)throw new Exception("Не удалось проверить установленный шрифт.");
        return newHash;
    }
    public void Restore(string game) {
        EnsureClosed(game);string resource=ResourcePath(game),backup=BackupPath(game),before=Hash(resource);
        if(before==SourceHash)return;
        if(!Known(before,game))throw new Exception("Файлы игры обновились. Возврат отменён, чтобы сохранить новую версию.");
        if(!File.Exists(backup) || Hash(backup)!=SourceHash)throw new Exception("Исходная копия игры не найдена.");
        string temporary=resource+".fontpatch-new";File.Copy(backup,temporary,true);
        EnsureClosed(game);if(Hash(resource)!=before)throw new Exception("Файл игры изменился. Попробуй ещё раз.");
        File.Replace(temporary,resource,null);if(Hash(resource)!=SourceHash)throw new Exception("Не удалось проверить восстановленный файл.");
    }
}
}
