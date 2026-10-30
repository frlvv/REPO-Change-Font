using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using RepoFonts;

static class Skin {
    public static readonly Color Background=Color.FromArgb(17,19,23), Surface=Color.FromArgb(26,29,35), Raised=Color.FromArgb(35,39,47), Border=Color.FromArgb(48,54,64), Text=Color.FromArgb(238,239,242), Muted=Color.FromArgb(139,146,159), Accent=Color.FromArgb(239,201,81), Green=Color.FromArgb(129,193,155);
    public static GraphicsPath Round(RectangleF box,float radius) {
        var p=new GraphicsPath();float d=radius*2;p.AddArc(box.X,box.Y,d,d,180,90);p.AddArc(box.Right-d,box.Y,d,d,270,90);p.AddArc(box.Right-d,box.Bottom-d,d,d,0,90);p.AddArc(box.X,box.Bottom-d,d,d,90,90);p.CloseFigure();return p;
    }
    public static void Shape(Graphics g,RectangleF rect,Color fill,Color border,float radius) {
        g.SmoothingMode=SmoothingMode.AntiAlias;using(var p=Round(rect,radius)) {using(var b=new SolidBrush(fill))g.FillPath(b,p);if(border!=Color.Empty)using(var pen=new Pen(border))g.DrawPath(pen,p);}
    }
}
class QuietButton : Button {
    public bool Primary, Ghost, Selected, Success, MutedWhenDisabled;
    public string WindowGlyph;
    bool hover,pressed;
    public QuietButton() {SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;Font=new Font("Segoe UI",10);BackColor=Skin.Background;ForeColor=Skin.Text;TabStop=true;}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;pressed=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnMouseDown(MouseEventArgs e){pressed=e.Button==MouseButtons.Left;Invalidate();base.OnMouseDown(e);}
    protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
    protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}
    protected override void OnLostFocus(EventArgs e){Invalidate();base.OnLostFocus(e);}
    protected override void OnPaint(PaintEventArgs e) {
        var g=e.Graphics;g.Clear(BackColor);Color fill=Ghost?Skin.Background:Primary?(Success?Skin.Green:Skin.Accent):Skin.Surface;
        if(hover)fill=WindowGlyph=="close"?Color.FromArgb(181,58,59):Primary?(Success?Color.FromArgb(148,209,172):Color.FromArgb(249,215,105)):Skin.Raised;
        if(!Enabled)fill=MutedWhenDisabled?Color.FromArgb(29,32,38):Skin.Surface;
        var r=new RectangleF(1,1,Width-3,Height-3);if(pressed)r.Inflate(-1,-1);
        Skin.Shape(g,r,fill,Ghost?Color.Empty:(!Enabled && MutedWhenDisabled?Color.FromArgb(38,42,49):Skin.Border),8);
        Color fg=!Enabled?(MutedWhenDisabled?Color.FromArgb(105,111,122):Skin.Muted):Primary?Skin.Background:Selected?Skin.Accent:Skin.Text;
        if(WindowGlyph!=null) {
            using(var pen=new Pen(fg,1.2F)) {float cx=Width/2F,cy=Height/2F;
                if(WindowGlyph=="close"){g.DrawLine(pen,cx-4,cy-4,cx+4,cy+4);g.DrawLine(pen,cx-4,cy+4,cx+4,cy-4);}
                else if(WindowGlyph=="min")g.DrawLine(pen,cx-5,cy+2,cx+5,cy+2);
                else if(WindowGlyph=="restore"){g.DrawRectangle(pen,cx-2,cy-5,7,7);using(var brush=new SolidBrush(fill))g.FillRectangle(brush,cx-5,cy-2,8,8);g.DrawRectangle(pen,cx-5,cy-2,7,7);}
                else g.DrawRectangle(pen,cx-4,cy-4,8,8);
            }
        } else TextRenderer.DrawText(g,Text,Font,Rectangle.Round(r),fg,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        if(Selected && Ghost)using(var pen=new Pen(Skin.Accent,2))g.DrawLine(pen,Width/2-7,Height-4,Width/2+7,Height-4);
        if(Focused && ShowFocusCues)Skin.Shape(g,new RectangleF(3,3,Width-7,Height-7),Color.Transparent,Skin.Accent,6);
    }
}
class ThemeLabel : Label {
    protected override void OnPaint(PaintEventArgs e){TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,ForeColor,TextFormatFlags.Left|TextFormatFlags.Top|TextFormatFlags.EndEllipsis);}
}
class HoldButton : QuietButton {
    readonly Timer repeat=new Timer();
    bool pointerGesture;
    public event EventHandler Step;
    public HoldButton(){repeat.Tick+=delegate{if(!Enabled || !Capture){repeat.Stop();return;}repeat.Interval=60;RaiseStep();};}
    void RaiseStep(){if(Enabled && Step!=null)Step(this,EventArgs.Empty);}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left && Enabled){pointerGesture=true;Capture=true;RaiseStep();repeat.Interval=350;repeat.Start();}}
    protected override void OnMouseUp(MouseEventArgs e){repeat.Stop();base.OnMouseUp(e);pointerGesture=false;}
    protected override void OnClick(EventArgs e){if(!pointerGesture)RaiseStep();base.OnClick(e);}
    protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture){repeat.Stop();if(IsHandleCreated && !Disposing && !IsDisposed)BeginInvoke((Action)delegate{pointerGesture=false;});}}
    protected override void OnLostFocus(EventArgs e){repeat.Stop();base.OnLostFocus(e);}
    protected override void OnEnabledChanged(EventArgs e){if(!Enabled){repeat.Stop();pointerGesture=false;}base.OnEnabledChanged(e);}
    protected override void OnKeyDown(KeyEventArgs e){pointerGesture=false;base.OnKeyDown(e);}
    protected override void Dispose(bool disposing){if(disposing)repeat.Dispose();base.Dispose(disposing);}
}
class FontModeSwitch : Control {
    bool separate=true,ru=true,mouseFocus;
    public bool Separate {get{return separate;}set{separate=value;Invalidate();}}
    public event Action<bool> Changed;
    public FontModeSwitch(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=Skin.Surface;Cursor=Cursors.Hand;TabStop=true;Font=new Font("Segoe UI",8.5F);}
    public void Localize(bool russian){ru=russian;AccessibleName=ru?"Источник обычного текста":"Body font source";Invalidate();}
    void SelectMode(bool next){if(next==separate)return;separate=next;Invalidate();if(Changed!=null)Changed(next);}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){mouseFocus=true;Focus();Invalidate();SelectMode(e.X>=Width*0.65F);}}
    protected override void OnKeyDown(KeyEventArgs e){mouseFocus=false;Invalidate();if(e.KeyCode==Keys.Left){SelectMode(false);e.Handled=true;}else if(e.KeyCode==Keys.Right){SelectMode(true);e.Handled=true;}else if(e.KeyCode==Keys.Space){SelectMode(!separate);e.Handled=true;}base.OnKeyDown(e);}
    protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}
    protected override void OnLostFocus(EventArgs e){mouseFocus=false;Invalidate();base.OnLostFocus(e);}
    protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(BackColor);Skin.Shape(g,new RectangleF(0.5F,0.5F,Width-1,Height-1),Skin.Background,Focused && ShowFocusCues && !mouseFocus?Skin.Accent:Skin.Border,7);int split=(int)(Width*0.65F);var left=new Rectangle(3,3,split-4,Height-6);var right=new Rectangle(split,3,Width-split-3,Height-6);Skin.Shape(g,separate?right:left,Skin.Raised,Color.Empty,5);TextRenderer.DrawText(g,ru?"Как у заголовка":"Same as heading",Font,left,separate?Skin.Muted:Skin.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);TextRenderer.DrawText(g,ru?"Отдельный":"Separate",Font,right,separate?Skin.Text:Skin.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
}
class PickerRenderer : ToolStripProfessionalRenderer { protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e){} }
class FontPicker : Control {
    readonly ToolStripDropDown popup; readonly ListBox list;
    readonly List<FontChoice> items=new List<FontChoice>(); int index=-1; bool mouseFocus,hover,popupMouse; string search=""; DateTime lastSearch;
    public event Action<string> Picked;
    public FontPicker(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Selectable,true);TabStop=true;Cursor=Cursors.Hand;Font=new Font("Segoe UI",10);BackColor=Skin.Surface;AccessibleRole=AccessibleRole.ComboBox;
        list=new ListBox{BorderStyle=BorderStyle.None,DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=34,IntegralHeight=false,BackColor=Skin.Surface,ForeColor=Skin.Text,Font=Font};
        list.DrawItem+=DrawRow;list.MouseMove+=delegate(object s,MouseEventArgs e){int i=list.IndexFromPoint(e.Location);if(i>=0 && i!=list.SelectedIndex)list.SelectedIndex=i;};
        list.MouseUp+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Left && list.IndexFromPoint(e.Location)>=0){popupMouse=true;Commit();}};
        list.KeyDown+=delegate(object s,KeyEventArgs e){if(e.KeyCode==Keys.Enter){popupMouse=false;Commit();e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Escape || e.KeyCode==Keys.F4 || (e.Alt && e.KeyCode==Keys.Up)){popupMouse=false;popup.Close();Focus();e.SuppressKeyPress=true;}};
        var host=new ToolStripControlHost(list){Margin=Padding.Empty,Padding=Padding.Empty,AutoSize=false};popup=new ToolStripDropDown{AutoSize=false,Padding=new Padding(4),Margin=Padding.Empty,BackColor=Skin.Surface,DropShadowEnabled=true,Renderer=new PickerRenderer()};popup.Items.Add(host);
    }
    void DrawRow(object sender,DrawItemEventArgs e){if(e.Index<0)return;bool selected=(e.State&DrawItemState.Selected)!=0;using(var bg=new SolidBrush(Skin.Surface))e.Graphics.FillRectangle(bg,e.Bounds);if(selected){var r=new RectangleF(e.Bounds.X+2,e.Bounds.Y+2,e.Bounds.Width-4,e.Bounds.Height-4);Skin.Shape(e.Graphics,r,Skin.Raised,Color.Empty,6);}if(e.Index==index)using(var p=new Pen(Skin.Accent,3))e.Graphics.DrawLine(p,e.Bounds.X+4,e.Bounds.Y+10,e.Bounds.X+4,e.Bounds.Bottom-10);var text=new Rectangle(e.Bounds.X+14,e.Bounds.Y,e.Bounds.Width-20,e.Bounds.Height);TextRenderer.DrawText(e.Graphics,items[e.Index].Name,Font,text,Skin.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);}
    public void SetItems(List<FontChoice> values,string path){items.Clear();items.AddRange(values);index=0;for(int i=0;i<items.Count;i++)if(string.Equals(items[i].Path,path,StringComparison.OrdinalIgnoreCase))index=i;list.BeginUpdate();list.Items.Clear();foreach(var item in items)list.Items.Add(item);list.SelectedIndex=index;list.EndUpdate();Invalidate();}
    void Open(){if(!Enabled || items.Count==0)return;list.SelectedIndex=index;int height=Math.Min(8,items.Count)*list.ItemHeight;list.Size=new Size(Width-8,height);popup.Items[0].Size=list.Size;popup.Size=new Size(Width,height+8);Region oldRegion=popup.Region;using(var shape=Skin.Round(new RectangleF(0,0,popup.Width,popup.Height),8))popup.Region=new Region(shape);if(oldRegion!=null)oldRegion.Dispose();popup.Show(this,new Point(0,Height+4));list.Focus();}
    void Commit(){if(list.SelectedIndex>=0){index=list.SelectedIndex;Invalidate();if(Picked!=null)Picked(items[index].Path);}popup.Close();mouseFocus=popupMouse;Focus();Invalidate();}
    protected override void OnMouseDown(MouseEventArgs e){mouseFocus=true;popupMouse=true;Focus();Invalidate();if(e.Button==MouseButtons.Left)Open();base.OnMouseDown(e);}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnEnabledChanged(EventArgs e){Cursor=Enabled?Cursors.Hand:Cursors.Default;if(!Enabled && popup!=null)popup.Close();Invalidate();base.OnEnabledChanged(e);}
    protected override void OnLostFocus(EventArgs e){if(!popup.Visible)mouseFocus=false;Invalidate();base.OnLostFocus(e);}
    protected override bool IsInputKey(Keys keyData){Keys k=keyData&Keys.KeyCode;if(k==Keys.Up || k==Keys.Down || k==Keys.Home || k==Keys.End)return true;return base.IsInputKey(keyData);}
    protected override void OnKeyDown(KeyEventArgs e){mouseFocus=false;Invalidate();if(e.KeyCode==Keys.Space || e.KeyCode==Keys.Enter || e.KeyCode==Keys.F4 || (e.Alt && e.KeyCode==Keys.Down)){popupMouse=false;Open();e.Handled=true;e.SuppressKeyPress=true;}else if(items.Count>0 && (e.KeyCode==Keys.Up || e.KeyCode==Keys.Down || e.KeyCode==Keys.Home || e.KeyCode==Keys.End)){int n=e.KeyCode==Keys.Home?0:e.KeyCode==Keys.End?items.Count-1:Math.Max(0,Math.Min(items.Count-1,index+(e.KeyCode==Keys.Up?-1:1)));if(n!=index){index=n;Invalidate();if(Picked!=null)Picked(items[index].Path);}e.Handled=true;}base.OnKeyDown(e);}
    protected override void OnKeyPress(KeyPressEventArgs e){if(!char.IsControl(e.KeyChar)){if((DateTime.Now-lastSearch).TotalSeconds>1)search="";search+=e.KeyChar;lastSearch=DateTime.Now;int n=items.FindIndex(x=>x.Name.StartsWith(search,StringComparison.CurrentCultureIgnoreCase));if(n>=0){index=n;Invalidate();if(Picked!=null)Picked(items[index].Path);}e.Handled=true;}base.OnKeyPress(e);}
    protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(BackColor);Color fill=Enabled?(hover?Color.FromArgb(42,47,56):Skin.Raised):Color.FromArgb(29,32,38);Color border=Enabled?(Focused && ShowFocusCues && !mouseFocus?Skin.Accent:Skin.Border):Color.FromArgb(38,42,49);Skin.Shape(g,new RectangleF(.5F,.5F,Width-1,Height-1),fill,border,8);Color fg=Enabled?Skin.Text:Color.FromArgb(105,111,122);if(index>=0 && index<items.Count)TextRenderer.DrawText(g,items[index].Name,Font,new Rectangle(12,0,Width-44,Height),fg,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);int x=Width-20,y=Height/2;using(var pen=new Pen(fg,1.3F)){g.DrawLine(pen,x-4,y-2,x,y+2);g.DrawLine(pen,x,y+2,x+4,y-2);}}
    protected override void Dispose(bool disposing){if(disposing)popup.Dispose();base.Dispose(disposing);}
}
class FontChoice { public string Name, Path; public override string ToString(){return Name;} }
class FontTile : Panel {
    readonly Label heading; readonly FontPicker choices; readonly QuietButton import; readonly FontModeSwitch mode;
    bool ru=true,drag,updating; public bool Optional;
    public event Action<string> Picked,Imported; public event Action<bool> ModeChanged;
    public FontTile(bool optional) {
        Optional=optional;DoubleBuffered=true;BackColor=Skin.Background;
        heading=new ThemeLabel{ForeColor=Skin.Muted,BackColor=Skin.Surface,Font=new Font("Segoe UI",10)};
        choices=new FontPicker{BackColor=Skin.Surface};choices.Picked+=delegate(string path){if(!updating && Picked!=null)Picked(path);};
        import=new QuietButton{BackColor=Skin.Surface,MutedWhenDisabled=true};import.Click+=delegate{PickFiles();};
        mode=new FontModeSwitch{Visible=!optional};mode.Changed+=delegate(bool value){choices.Enabled=import.Enabled=value;if(ModeChanged!=null)ModeChanged(value);};
        Controls.AddRange(new Control[]{heading,choices,import,mode});Hook(this);Hook(heading);Hook(choices);Hook(import);Resize+=delegate{LayoutTile();};Localize(true);
    }
    void Hook(Control c){c.AllowDrop=true;c.DragEnter+=delegate(object s,DragEventArgs e){if(e.Data.GetDataPresent(DataFormats.FileDrop)){drag=true;Invalidate();e.Effect=DragDropEffects.Copy;}};c.DragLeave+=delegate{drag=false;Invalidate();};c.DragDrop+=delegate(object s,DragEventArgs e){drag=false;Invalidate();if(!Optional && !mode.Separate)return;foreach(string path in (string[])e.Data.GetData(DataFormats.FileDrop))if(Imported!=null)Imported(path);};}
    void PickFiles(){using(var d=new OpenFileDialog{Title=heading.Text,Filter=ru?"Шрифты (*.ttf;*.otf)|*.ttf;*.otf":"Fonts (*.ttf;*.otf)|*.ttf;*.otf",CheckFileExists=true,Multiselect=true})if(d.ShowDialog()==DialogResult.OK)foreach(string file in d.FileNames)if(Imported!=null)Imported(file);}
    void LayoutTile(){heading.SetBounds(20,17,Optional?Width-40:Width-252,23);choices.SetBounds(20,58,Width-150,36);import.SetBounds(Width-120,58,100,36);mode.SetBounds(Width-234,12,214,31);}
    public void Localize(bool russian){ru=russian;heading.Text=Optional?(ru?"Заголовки":"Headings"):(ru?"Обычный текст":"Body text");import.Text=ru?"Добавить…":"Import…";mode.Localize(ru);choices.AccessibleName=heading.Text;AccessibleName=heading.Text;}
    public void SetSeparate(bool value){mode.Separate=value;choices.Enabled=import.Enabled=Optional || value;import.Cursor=import.Enabled?Cursors.Hand:Cursors.Default;}
    public void SetChoices(List<FontChoice> items,string path){updating=true;choices.SetItems(items,path);updating=false;}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);Skin.Shape(e.Graphics,new RectangleF(1,1,Width-3,Height-3),Skin.Surface,drag?Skin.Accent:Skin.Border,12);}
}
class SpacingInput : Panel {
    readonly HoldButton minus,plus;
    readonly TextBox valueText;
    readonly Label percent;
    decimal value=1;
    bool ru=true,updating;
    public event EventHandler ValueChanged;
    public decimal Value {get{return value;}set{SetValue(value);}}
    public SpacingInput(){DoubleBuffered=true;BackColor=Skin.Background;Size=new Size(160,42);
        minus=new HoldButton{Text="−",Ghost=true,BackColor=Skin.Surface};plus=new HoldButton{Text="+",Ghost=true,BackColor=Skin.Surface};
        valueText=new TextBox{BorderStyle=BorderStyle.None,BackColor=Skin.Surface,ForeColor=Skin.Text,TextAlign=HorizontalAlignment.Center,Font=new Font("Segoe UI",11)};
        percent=new ThemeLabel{Text="%",BackColor=Skin.Surface,ForeColor=Skin.Muted,Font=new Font("Segoe UI",10)};
        Controls.AddRange(new Control[]{minus,plus,valueText,percent});minus.Step+=delegate{SetValue(value-0.1M);};plus.Step+=delegate{SetValue(value+0.1M);};
        valueText.Leave+=delegate{Commit();};valueText.KeyDown+=delegate(object s,KeyEventArgs e){if(e.KeyCode==Keys.Enter){Commit();e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Up){SetValue(value+0.1M);e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Down){SetValue(value-0.1M);e.SuppressKeyPress=true;}};
        Resize+=delegate{Arrange();};Arrange();Localize(true);}
    void Arrange(){minus.SetBounds(3,3,34,36);plus.SetBounds(Width-37,3,34,36);valueText.SetBounds(39,10,54,24);percent.SetBounds(95,11,20,24);}
    void Commit(){if(updating)return;decimal parsed;if(decimal.TryParse(valueText.Text.Replace(',','.'),NumberStyles.Number,CultureInfo.InvariantCulture,out parsed))SetValue(parsed);else RefreshText();}
    public void CommitValue(){Commit();}
    void SetValue(decimal next){next=Math.Round(Math.Max(0,Math.Min(10,next)),1);bool changed=next!=value;value=next;RefreshText();if(changed && ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}
    void RefreshText(){updating=true;valueText.Text=value.ToString("0.0",ru?CultureInfo.GetCultureInfo("ru-RU"):CultureInfo.InvariantCulture);updating=false;}
    public void Localize(bool russian){ru=russian;minus.AccessibleName=ru?"Уменьшить расстояние":"Decrease letter spacing";plus.AccessibleName=ru?"Увеличить расстояние":"Increase letter spacing";valueText.AccessibleName=ru?"Расстояние между буквами":"Letter spacing";RefreshText();}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);Skin.Shape(e.Graphics,new RectangleF(1,1,Width-3,Height-3),Skin.Surface,Skin.Border,8);}
}
class PreviewFace : IDisposable {
    readonly PrivateFontCollection collection=new PrivateFontCollection();
    IntPtr memory;
    FontFamily family;
    FontStyle actualStyle;
    public bool Available;
    public PreviewFace(byte[] data){try{memory=Marshal.AllocHGlobal(data.Length);Marshal.Copy(data,0,memory,data.Length);collection.AddMemoryFont(memory,data.Length);family=collection.Families.FirstOrDefault();if(family==null)return;foreach(FontStyle style in new FontStyle[]{FontStyle.Regular,FontStyle.Bold,FontStyle.Italic,FontStyle.Bold|FontStyle.Italic})if(family.IsStyleAvailable(style)){actualStyle=style;Available=true;break;}}catch{Available=false;}}
    public Font Font(float size){return new Font(family,size,actualStyle,GraphicsUnit.Pixel);}
    public void Dispose(){collection.Dispose();if(memory!=IntPtr.Zero){Marshal.FreeHGlobal(memory);memory=IntPtr.Zero;}}
}
class FontPreview : Control {
    public PreviewFace Regular,Bold;
    public bool Ru=true,Separate=true;
    public string Sample="Собирай. Выживай. Забирай.";
    public double Spacing=1;
    public FontPreview(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=Skin.Background;AccessibleName="Предпросмотр шрифта";}
    void Tracked(Graphics g,string text,Font f,Color color,float y,float maxWidth){
        using(var format=(StringFormat)StringFormat.GenericTypographic.Clone())using(var brush=new SolidBrush(color)) {
            format.FormatFlags|=StringFormatFlags.MeasureTrailingSpaces;
            float x=24,extra=f.Size*(float)Spacing/100;
            var chars=StringInfo.GetTextElementEnumerator(text);
            while(chars.MoveNext()){string ch=chars.GetTextElement();float advance=g.MeasureString(ch,f,10000,format).Width;if(x+advance>maxWidth+24)break;g.DrawString(ch,f,brush,x,y,format);x+=advance+extra;}
        }
    }
    protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Skin.Background);Skin.Shape(g,new RectangleF(1,1,Width-3,Height-3),Skin.Surface,Skin.Border,12);
        TextRenderer.DrawText(g,Ru?"Предпросмотр":"Preview",Font,new Point(23,18),Skin.Muted);
        if(Regular==null || !Regular.Available || (Separate && (Bold==null || !Bold.Available))){TextRenderer.DrawText(g,Ru?"Предпросмотр недоступен для этого файла":"Preview is unavailable for this font",Font,new Rectangle(24,60,Width-48,Height-80),Skin.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return;}
        g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;var face=Separate?Bold:Regular;float titleSize=48;
        using(var measure=face.Font(titleSize)){float estimated=g.MeasureString(Sample,measure).Width+Sample.Length*titleSize*(float)Spacing/100;if(estimated>Width-48)titleSize=Math.Max(17,titleSize*(Width-48)/estimated);}
        float top=Math.Max(63,(Height-166)/2F+20);
        using(var title=face.Font(titleSize))Tracked(g,Sample,title,Skin.Text,top,Width-48);
        using(var body=Regular.Font(27))Tracked(g,"Аа Бб Вв Гг Дд – 0123456789",body,Skin.Text,top+79,Width-48);
        using(var english=Regular.Font(23))Tracked(g,"The quick brown fox jumps over the lazy dog.",english,Skin.Muted,top+125,Width-48);
    }
}
public class UiPrefs {public bool Ru=true,Separate=true,HeadingRegularPreset;public bool? BodyMatchesHeading;public decimal Spacing=1;public string Game,Regular,Bold;}
class ChangeFontWindow : Form {
    const string AppName="R.E.P.O. Change Font";
    readonly Engine engine;
    readonly Stream windowIconStream;
    readonly FontTile bodyTile,headingTile;
    readonly FontPreview preview;
    readonly SpacingInput spacing;
    readonly Panel separator;
    readonly Label spacingLabel,gameLabel,status,title;
    readonly LinkLabel official; const string OfficialSource="https://github.com/frlvv/REPO-Change-Font"; readonly string libraryPath; List<FontChoice> libraryEntries;
    readonly PictureBox appImage;
    readonly QuietButton apply,restore,chooseGame,ruButton,enButton,minButton,maxButton,closeButton;
    PreviewFace regularFace,boldFace;
    string game,regularPath,boldPath;
    bool ru=true,separate=true,busy,loading=true,applied,headingRegularPreset;
    readonly string prefsPath;
    [DllImport("user32.dll")]static extern bool ReleaseCapture();
    [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr hwnd,int msg,int wParam,int lParam);
    public ChangeFontWindow(string[] args,string prefsOverride=null,string libraryOverride=null) {
        prefsPath=prefsOverride??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"REPO Change Font","preferences.json");
        libraryPath=libraryOverride??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"REPO Change Font","Fonts");engine=new Engine();var prefs=LoadPrefs();ru=prefs.Ru;separate=prefs.BodyMatchesHeading.HasValue?!prefs.BodyMatchesHeading.Value:prefs.Separate;headingRegularPreset=prefs.HeadingRegularPreset;
        game=File.Exists(Path.Combine(prefs.Game??"","REPO.exe"))?prefs.Game:Engine.FindGame();
        regularPath=Migrate(prefs.Regular);boldPath=Migrate(prefs.Bold);
        if(!prefs.BodyMatchesHeading.HasValue && !prefs.Separate){boldPath=regularPath;headingRegularPreset=regularPath==null;}
        regularFace=new PreviewFace(regularPath==null?engine.DefaultFont(false):File.ReadAllBytes(regularPath));
        boldFace=new PreviewFace(boldPath==null?engine.DefaultFont(!headingRegularPreset):File.ReadAllBytes(boldPath));
        Text=AppName;BackColor=Skin.Background;ForeColor=Skin.Text;Font=new Font("Segoe UI",10);FormBorderStyle=FormBorderStyle.None;
        ClientSize=new Size(920,736);MinimumSize=new Size(820,704);StartPosition=FormStartPosition.CenterScreen;DoubleBuffered=true;
        windowIconStream=new MemoryStream();using(var resource=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("RepoFontIcon"))resource.CopyTo(windowIconStream);windowIconStream.Position=0;Icon=new Icon(windowIconStream,32,32);
        title=new Label{Text=AppName,BackColor=Skin.Background,ForeColor=Skin.Text,Font=new Font("Segoe UI",10,FontStyle.Bold)};
        appImage=new PictureBox{BackColor=Skin.Background,Image=Icon.ToBitmap(),SizeMode=PictureBoxSizeMode.Zoom};
        minButton=new QuietButton{Ghost=true,WindowGlyph="min"};maxButton=new QuietButton{Ghost=true,WindowGlyph="max"};closeButton=new QuietButton{Ghost=true,WindowGlyph="close"};
        ruButton=new QuietButton{Ghost=true,Text="RU"};enButton=new QuietButton{Ghost=true,Text="EN"};
        minButton.Click+=delegate{WindowState=FormWindowState.Minimized;};maxButton.Click+=delegate{ToggleMax();};closeButton.Click+=delegate{Close();};
        ruButton.Click+=delegate{ru=true;Localize();SavePrefs();};enButton.Click+=delegate{ru=false;Localize();SavePrefs();};
        MouseDown+=DragHeader;title.MouseDown+=DragHeader;appImage.MouseDown+=DragHeader;title.DoubleClick+=delegate{ToggleMax();};appImage.DoubleClick+=delegate{ToggleMax();};
        bodyTile=new FontTile(false);headingTile=new FontTile(true);
        bodyTile.Picked+=PickRegular;headingTile.Picked+=PickBold;bodyTile.Imported+=delegate(string p){ImportFont(p,false);};headingTile.Imported+=delegate(string p){ImportFont(p,true);};
        bodyTile.ModeChanged+=delegate(bool own){separate=own;MarkPending();UpdateTiles();RefreshPreview();SavePrefs();};
        preview=new FontPreview{Font=Font};
        spacing=new SpacingInput();spacing.Value=Math.Max(0,Math.Min(10,prefs.Spacing));spacing.ValueChanged+=delegate{MarkPending();RefreshPreview();SavePrefs();};
        spacingLabel=new Label{ForeColor=Skin.Text,BackColor=Skin.Background};
        separator=new Panel{BackColor=Skin.Border};
        gameLabel=new Label{ForeColor=Skin.Muted,BackColor=Skin.Background};
        chooseGame=new QuietButton{Ghost=true};chooseGame.Click+=delegate{using(var dialog=new FolderBrowserDialog{Description=ru?"Выбери папку R.E.P.O.":"Choose your R.E.P.O. folder",SelectedPath=game,ShowNewFolderButton=false})if(dialog.ShowDialog()==DialogResult.OK){game=dialog.SelectedPath;MarkPending();UpdateGame();SavePrefs();}};
        apply=new QuietButton{Primary=true};restore=new QuietButton();
        apply.Click+=async delegate{await Run(false);};restore.Click+=async delegate{await Run(true);};
        status=new Label{ForeColor=Skin.Muted,BackColor=Skin.Background,Font=new Font("Segoe UI",9)};official=new LinkLabel{LinkColor=Skin.Muted,ActiveLinkColor=Skin.Accent,VisitedLinkColor=Skin.Muted,BackColor=Skin.Background,TextAlign=ContentAlignment.MiddleRight,Font=new Font("Segoe UI",9),LinkBehavior=LinkBehavior.HoverUnderline};official.LinkClicked+=delegate{System.Diagnostics.Process.Start(OfficialSource);};
        Controls.AddRange(new Control[]{title,appImage,minButton,maxButton,closeButton,ruButton,enButton,headingTile,bodyTile,preview,spacingLabel,spacing,separator,gameLabel,chooseGame,apply,restore,status,official});
        minButton.TabStop=maxButton.TabStop=closeButton.TabStop=preview.TabStop=false;
        headingTile.TabIndex=0;bodyTile.TabIndex=1;spacing.TabIndex=2;chooseGame.TabIndex=3;apply.TabIndex=4;restore.TabIndex=5;ruButton.TabIndex=6;enButton.TabIndex=7;
        Resize+=delegate{LayoutWindow();};FormClosing+=delegate(object s,FormClosingEventArgs e){if(busy)e.Cancel=true;else SavePrefs();};
        FormClosed+=delegate{regularFace.Dispose();boldFace.Dispose();appImage.Image.Dispose();Icon.Dispose();windowIconStream.Dispose();};
        LayoutWindow();Localize();loading=false;
        if(args.Length>0 && File.Exists(args[0]))ImportFont(args[0],false);if(args.Length>1 && File.Exists(args[1]))ImportFont(args[1],true);
    }
    void DragHeader(object sender,MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;if(sender==this && e.Y>54)return;ReleaseCapture();SendMessage(Handle,0xA1,2,0);}
    void ToggleMax(){if(WindowState==FormWindowState.Maximized)WindowState=FormWindowState.Normal;else{MaximizedBounds=Screen.FromControl(this).WorkingArea;WindowState=FormWindowState.Maximized;}maxButton.WindowGlyph=WindowState==FormWindowState.Maximized?"restore":"max";maxButton.Invalidate();LayoutWindow();}
    protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0x84 && WindowState==FormWindowState.Normal && (int)m.Result==1){long pos=m.LParam.ToInt64();Point p=PointToClient(new Point((short)(pos&65535),(short)((pos>>16)&65535)));int edge=6;bool l=p.X<edge,r=p.X>=Width-edge,t=p.Y<edge,b=p.Y>=Height-edge;if(l&&t)m.Result=(IntPtr)13;else if(r&&t)m.Result=(IntPtr)14;else if(l&&b)m.Result=(IntPtr)16;else if(r&&b)m.Result=(IntPtr)17;else if(l)m.Result=(IntPtr)10;else if(r)m.Result=(IntPtr)11;else if(t)m.Result=(IntPtr)12;else if(b)m.Result=(IntPtr)15;}}
    void LayoutWindow(){if(title==null || bodyTile==null)return;int w=ClientSize.Width,h=ClientSize.Height,pad=28,gap=20,cw=(w-pad*2-gap)/2,previewH=Math.Max(220,h-484);
        appImage.SetBounds(20,15,24,24);title.SetBounds(55,17,Math.Max(230,w-340),24);
        closeButton.SetBounds(w-48,10,36,34);maxButton.SetBounds(w-88,10,36,34);minButton.SetBounds(w-128,10,36,34);enButton.SetBounds(w-181,10,36,34);ruButton.SetBounds(w-219,10,36,34);
        headingTile.SetBounds(pad,80,cw,132);bodyTile.SetBounds(pad+cw+gap,80,cw,132);
        preview.SetBounds(pad,232,w-pad*2,previewH);
        int settingsY=232+previewH+24;spacingLabel.SetBounds(pad,settingsY+10,w-260,30);spacing.SetBounds(w-pad-160,settingsY,160,42);
        int lineY=settingsY+64;separator.SetBounds(pad,lineY,w-pad*2,1);gameLabel.SetBounds(pad,lineY+24,w-pad*2-180,27);chooseGame.SetBounds(w-pad-158,lineY+14,158,37);
        int actionY=lineY+62;apply.SetBounds(pad,actionY,cw,46);restore.SetBounds(pad+cw+gap,actionY,cw,46);status.SetBounds(pad,actionY+60,w/2-pad,24);official.SetBounds(w/2,actionY+60,w/2-pad,24);
        if(Width>0 && Height>0){Region old=Region;using(var path=Skin.Round(new RectangleF(0,0,Width,Height),WindowState==FormWindowState.Maximized?1:12))Region=new Region(path);if(old!=null)old.Dispose();}Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);Skin.Shape(e.Graphics,new RectangleF(0.5F,0.5F,Width-1,Height-1),Color.Transparent,Skin.Border,WindowState==FormWindowState.Maximized?1:12);}
    string T(string russian,string english){return ru?russian:english;}
    void Localize(){bodyTile.Localize(ru);headingTile.Localize(ru);spacing.Localize(ru);ruButton.Selected=ru;enButton.Selected=!ru;ruButton.Invalidate();enButton.Invalidate();
        official.Text=T("Оригинал – только на GitHub","Official release – only on GitHub");spacingLabel.Text=T("Расстояние между буквами","Letter spacing");UpdateApplyButton();restore.Text=T("Вернуть исходный","Restore original");chooseGame.Text=T("Выбрать папку","Choose folder");
        minButton.AccessibleName=T("Свернуть","Minimize");maxButton.AccessibleName=T("Развернуть или восстановить","Maximize or restore");closeButton.AccessibleName=T("Закрыть","Close");
        preview.AccessibleName=T("Предпросмотр шрифтов","Font preview");
        status.Text=applied?"":T("Готово","Ready");status.ForeColor=Skin.Muted;UpdateTiles();UpdateGame();RefreshPreview();
    }
    List<FontChoice> Library(){if(libraryEntries!=null)return libraryEntries;var items=new List<FontChoice>{new FontChoice{Name="Sexsmith",Path=null}};try{if(Directory.Exists(libraryPath))foreach(string file in Directory.GetFiles(libraryPath)){try{byte[] bytes=File.ReadAllBytes(file);using(var sha=System.Security.Cryptography.SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").Equals("C9B990D978EFA939177F8CF8743F78247AB8D8D5E140C7092157A1E274BABF83",StringComparison.OrdinalIgnoreCase))continue;var f=FontInfo.Read(bytes);items.Add(new FontChoice{Name=f.Name,Path=file});}catch{}}}catch{}libraryEntries=items;return items;}
    string ImportPath(string path){string ext=Path.GetExtension(path).ToLowerInvariant();if(ext!=".ttf" && ext!=".otf")throw new Exception("Выбери файл шрифта TTF или OTF.");byte[] bytes=File.ReadAllBytes(path);FontInfo.Read(bytes);using(var face=new PreviewFace(bytes))if(!face.Available)throw new Exception("Не удалось прочитать шрифт.");Directory.CreateDirectory(libraryPath);string name=Path.GetFileNameWithoutExtension(path);foreach(char c in Path.GetInvalidFileNameChars())name=name.Replace(c,'_');if(name.Length>60)name=name.Substring(0,60);string hash;using(var sha=System.Security.Cryptography.SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();if(hash=="c9b990d978efa939177f8cf8743f78247ab8d8d5e140c7092157a1e274babf83")return null;hash=hash.Substring(0,16);if(string.Equals(Path.GetDirectoryName(Path.GetFullPath(path)).TrimEnd(Path.DirectorySeparatorChar),Path.GetFullPath(libraryPath).TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase))return Path.GetFullPath(path);string dest=Path.Combine(libraryPath,name+"-"+hash+ext);if(!File.Exists(dest))File.WriteAllBytes(dest,bytes);libraryEntries=null;return dest;}
    string Migrate(string path){try{return !string.IsNullOrEmpty(path) && File.Exists(path)?ImportPath(path):null;}catch{return null;}}
    void ImportFont(string path,bool heading){try{string saved=ImportPath(path);if(heading)PickBold(saved);else PickRegular(saved);}catch(Exception e){ShowError(e);}}
    void UpdateTiles(){var items=Library();headingTile.SetChoices(items,boldPath);bodyTile.SetSeparate(separate);bodyTile.SetChoices(items,separate?regularPath:boldPath);}
    void UpdateGame(){bool found=File.Exists(Path.Combine(game??"","REPO.exe"));gameLabel.Text=found?T("●  Игра найдена","●  Game found"):T("Выбери папку игры","Choose your game folder");gameLabel.ForeColor=found?Skin.Green:Skin.Muted;}
    void RefreshPreview(){if(preview==null || spacing==null)return;preview.Regular=separate?regularFace:boldFace;preview.Bold=boldFace;preview.Separate=separate;preview.Ru=ru;preview.Spacing=(double)spacing.Value;preview.Sample=T("Собирай. Выживай. Забирай.","Collect. Survive. Extract.");preview.Invalidate();}
    void PickRegular(string path){try{byte[] bytes=path==null?engine.DefaultFont(false):File.ReadAllBytes(path);FontInfo.Read(bytes);var next=new PreviewFace(bytes);var old=regularFace;regularFace=next;regularPath=path==null?null:Path.GetFullPath(path);separate=true;MarkPending();UpdateTiles();RefreshPreview();old.Dispose();SavePrefs();}catch(Exception e){ShowError(e);}}
    void PickBold(string path){try{byte[] bytes=path==null?engine.DefaultFont(true):File.ReadAllBytes(path);FontInfo.Read(bytes);var next=new PreviewFace(bytes);var old=boldFace;boldFace=next;boldPath=path==null?null:Path.GetFullPath(path);headingRegularPreset=false;MarkPending();UpdateTiles();RefreshPreview();old.Dispose();SavePrefs();}catch(Exception e){ShowError(e);}}
    UiPrefs LoadPrefs(){try{if(File.Exists(prefsPath)){var saved=new JavaScriptSerializer().Deserialize<UiPrefs>(File.ReadAllText(prefsPath));if(saved!=null)return saved;}}catch{}return new UiPrefs();}
    void SavePrefs(){if(loading)return;try{Directory.CreateDirectory(Path.GetDirectoryName(prefsPath));var p=new UiPrefs{Ru=ru,Separate=separate,BodyMatchesHeading=!separate,HeadingRegularPreset=headingRegularPreset,Spacing=spacing.Value,Game=game,Regular=regularPath,Bold=boldPath};File.WriteAllText(prefsPath,new JavaScriptSerializer().Serialize(p),Encoding.UTF8);}catch{}}
    string ErrorText(Exception e){
        if(e is UnauthorizedAccessException)return T("Нет прав на изменение файлов игры.","You don't have permission to change the game files.");
        if(ru)return e.Message;
        var messages=new Dictionary<string,string>{
            {"Закрой игру и попробуй ещё раз.","Close the game and try again."},
            {"Эта версия игры пока не поддерживается.","This game version is not supported yet."},
            {"Эта версия игры пока не поддерживается. Файлы оставлены как есть.","This game version is not supported yet. Your files haven't been changed."},
            {"Файлы игры обновились. Возврат отменён, чтобы сохранить новую версию.","The game files have been updated. Restore was cancelled to keep the newer version."},
            {"Исходная копия игры не найдена.","The original game backup couldn't be found."},
            {"Не найдена исходная копия игры для замены шрифта.","The original game backup is needed to change the font."},
            {"Не удалось проверить исходную копию игры.","The original game backup couldn't be verified."},
            {"Файл игры изменился. Попробуй ещё раз.","The game file changed. Please try again."},
            {"Выбери файл шрифта TTF или OTF.","Choose a TTF or OTF font file."},
            {"Этот формат шрифта пока не поддерживается. Нужен TTF или OTF.","This font format is not supported. Choose a TTF or OTF file."},
            {"Файл шрифта повреждён.","The font file appears to be damaged."},
            {"Не удалось прочитать шрифт.","The font couldn't be read."},
            {"В шрифте не хватает необходимых данных.","This font is missing required data."},
            {"Не удалось определить размер шрифта.","The font metrics couldn't be read."},
            {"Проверь межбуквенный интервал.","Check the letter spacing value."},
            {"Не удалось проверить установленный шрифт.","The installed font couldn't be verified."},
            {"Не удалось проверить восстановленный файл.","The restored game file couldn't be verified."}};
        string text;return messages.TryGetValue(e.Message,out text)?text:"Couldn't apply the changes. Check that the game is closed and try again.";
    }
    void ShowError(Exception e){status.Text=T("Не удалось применить изменения","Couldn't apply the changes");status.ForeColor=Skin.Muted;MessageBox.Show(this,ErrorText(e),AppName,MessageBoxButtons.OK,MessageBoxIcon.Information);}
    void UpdateApplyButton(){if(apply==null)return;apply.Success=applied;apply.Text=applied?T("Применено","Applied"):T("Применить","Apply");apply.Invalidate();}
    void MarkPending(){applied=false;UpdateApplyButton();if(status!=null)status.Text=T("Готово","Ready");}
    void SetBusy(bool value){busy=value;foreach(Control c in new Control[]{bodyTile,headingTile,spacing,apply,restore,chooseGame,ruButton,enButton})c.Enabled=!value;}
    async Task Run(bool restoring){if(string.IsNullOrEmpty(game) || !File.Exists(Path.Combine(game,"REPO.exe"))){MessageBox.Show(this,T("Сначала выбери папку игры.","Choose your game folder first."),AppName);return;}spacing.CommitValue();MarkPending();var options=new Options{RegularPath=regularPath,BoldPath=boldPath,SeparateBold=separate,BodyMatchesHeading=!separate,HeadingRegularPreset=headingRegularPreset,Spacing=(double)spacing.Value};SetBusy(true);status.ForeColor=Skin.Muted;status.Text=restoring?T("Возвращаю исходный шрифт…","Restoring the original font…"):"";if(!restoring)apply.Text=T("Применяется…","Applying…");
        try{await Task.Run(delegate{if(restoring)engine.Restore(game);else engine.Install(game,options);});applied=!restoring;UpdateApplyButton();status.Text=restoring?T("Исходный шрифт возвращён","Original font restored"):"";status.ForeColor=Skin.Green;SavePrefs();}
        catch(Exception e){ShowError(e);}finally{SetBusy(false);UpdateApplyButton();}
    }
}
static class Program {
    [STAThread]static void Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);try{Application.Run(new ChangeFontWindow(args));}catch(Exception e){MessageBox.Show(e.Message,"R.E.P.O. Change Font",MessageBoxButtons.OK,MessageBoxIcon.Information);}}
}

