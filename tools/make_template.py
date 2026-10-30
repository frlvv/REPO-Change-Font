import copy,gzip,json,pathlib,struct
import argparse
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
base=pathlib.Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--game-path', required=True, type=pathlib.Path)
parser.add_argument('--source', required=True, type=pathlib.Path)
parser.add_argument('--output', required=True, type=pathlib.Path)
args=parser.parse_args()
root=args.game_path
src=args.source
patched=args.output/'sharedassets0.assets'
gen=TypeTreeGenerator('6000.3.17f1');gen.load_local_game(str(root))
node=gen.get_nodes_up('Unity.TextMeshPro.dll','TMPro.TMP_FontAsset')
a=UnityPy.load(str(src)).file;b=UnityPy.load(str(patched)).file;raw=patched.read_bytes()
offset=b.header.data_offset
header=raw[:offset]
objects=[]
for ident,obj in sorted(b.objects.items()):
    marker=struct.pack('<qqIi',ident,obj.byte_start-offset,obj.byte_size,obj.type_id)
    pos=header.find(marker)
    assert pos>=0 and header.find(marker,pos+1)<0,ident
    d={'id':ident,'slot':pos+8}
    old=a.objects.get(ident)
    if old and old.get_raw_data()==obj.get_raw_data():d.update(sourceOffset=old.byte_start,sourceLength=old.byte_size)
    else:d['data']=list(obj.get_raw_data())
    objects.append(d)
def probe_offset(objid, mutate, probe, fmt):
    obj=b.objects[objid];before=obj.get_raw_data();d=obj.read_typetree(node)
    mutate(d,probe);after=obj.save_typetree(d,node);needle=struct.pack(fmt,probe)
    start=after.find(needle)
    assert start>=0 and after.find(needle,start+1)<0,(objid,probe,start,after.find(needle,start+1),len(before),len(after))
    obj.set_raw_data(before);return start
font_offsets=[]
for ident,fontid,style in [(14291,1408,'Medium'),(19116,19113,'Bold')]:
    face={}
    for i,key in enumerate(['m_PointSize','m_Scale','m_UnitsPerEM','m_LineHeight','m_AscentLine','m_CapLine','m_MeanLine','m_Baseline','m_DescentLine','m_SuperscriptOffset','m_SubscriptOffset','m_UnderlineOffset','m_UnderlineThickness','m_StrikethroughOffset','m_StrikethroughThickness','m_TabWidth']):
        integer=key=='m_UnitsPerEM';probe=1234567 if integer else 123.456+i
        face[key]=probe_offset(ident,lambda d,p,key=key:d['m_FaceInfo'].__setitem__(key,p),probe,'<i' if integer else '<f')
    fields={key:probe_offset(ident,lambda d,p,key=key:d.__setitem__(key,p),134.456+i,'<f') for i,key in enumerate(['normalStyle','boldStyle','normalSpacingOffset','boldSpacing'])}
    weights=[probe_offset(ident,lambda d,p,i=i:d['m_FontWeightTable'][i]['regularTypeface'].__setitem__('m_PathID',p),123456789012345+i,'<q') for i in range(6,10)]
    fontbytes=(base/'assets/Sexsmith.otf').read_bytes();fontobj=b.objects[fontid].get_raw_data();at=fontobj.find(fontbytes)
    assert at>=4 and struct.unpack_from('<i',fontobj,at-4)[0]==len(fontbytes)
    objects[[o['id'] for o in objects].index(fontid)].pop('data',None)
    default_font=(base/'assets/Sexsmith.otf').read_bytes()
    font_offsets.append({'assetId':ident,'fontId':fontid,'style':style,'face':face,'fields':fields,'weightSlots':weights,'fontPrefix':list(fontobj[:at-4]),'fontSuffix':list(fontobj[(at+len(fontbytes)+3)//4*4:]),'fontBytes':list(default_font)})
materials=[]
for ident in [2,114,115,117,19115]:
    obj=b.objects[ident];before=obj.get_raw_data();data=obj.read_typetree();slots={}
    for i,key in enumerate(['_WeightNormal','_WeightBold']):
        d=copy.deepcopy(data);probe=158.456+i
        d['m_SavedProperties']['m_Floats']=[(k,probe if k==key else v) for k,v in d['m_SavedProperties']['m_Floats']]
        after=obj.save_typetree(d);needle=struct.pack('<f',probe);at=after.find(needle)
        assert at>=0 and after.find(needle,at+1)<0;slots[key]=at;obj.set_raw_data(before)
    materials.append({'id':ident,'bold':ident==19115,'slots':slots})
manifest=json.loads((args.output/'manifest.json').read_text())
template={'header':list(header),'dataOffset':offset,'objects':objects,'fonts':font_offsets,'materials':materials,'sourceHash':manifest['source_sha256'],'knownHashes':[manifest['patched_sha256'],'793a07e2971c6d4b9d2f25064a5464d9009f7d3a2cdd8e34b369f7eb56bdebbf','c3a66bcabb2ef463b11f46cbdf515b106190c7d6e913ecc681ba9fc7e62415eb'],'build':'26294889'}
packed=gzip.compress(json.dumps(template,separators=(',',':')).encode(),9,mtime=0)
(args.output/'app-template.json.gz').write_bytes(packed)
print('Template bytes',len(packed),'objects',len(objects),'header',len(header))
