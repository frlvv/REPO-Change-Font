import copy,hashlib,io,json,pathlib,sys,uuid
import argparse
import UnityPy
from fontTools.ttLib import TTFont
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator

sys.stdout.reconfigure(encoding='utf-8')
base=pathlib.Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--game-path', required=True, type=pathlib.Path)
parser.add_argument('--source', required=True, type=pathlib.Path)
parser.add_argument('--output', required=True, type=pathlib.Path)
args=parser.parse_args()
root=args.game_path
source=args.source
target=args.output/'sharedassets0.assets'; target.parent.mkdir(parents=True,exist_ok=True)
assert hashlib.sha256(source.read_bytes()).hexdigest()=='531d9fe2f51fc526e16358645e51aa1aa2ed5ea89c49c4a115b15bb989e4dd4b', 'Unsupported original game assets'
gen=TypeTreeGenerator('6000.3.17f1'); gen.load_local_game(str(root))
node=gen.get_nodes_up('Unity.TextMeshPro.dll','TMPro.TMP_FontAsset')
env=UnityPy.load(str(source)); asset=env.file
originals={i:o.get_raw_data() for i,o in asset.objects.items()}
original_trees={i:asset.objects[i].read_typetree(node) for i in [14290,14291,14293]}
next_id=max(asset.objects)+1
def clone(ident):
    global next_id
    obj=copy.copy(asset.objects[ident]); obj.path_id=next_id; next_id+=1
    obj.data=None; obj._read_until=None
    obj.set_raw_data(originals[ident]); asset.objects[obj.path_id]=obj
    return obj
def ptr(obj): return {'m_FileID':0,'m_PathID':obj.path_id}
def verify_roundtrip(obj, tree_node=None):
    before=obj.get_raw_data(); data=obj.read_typetree(tree_node)
    obj.save_typetree(data,tree_node)
    assert before==obj.get_raw_data(), f'Roundtrip differs: {obj.path_id}'
    return data
for i in [1408,432,429,2,14290,14291,14293]:
    verify_roundtrip(asset.objects[i],node if i in original_trees else None)
variants=[]
primary_scale=56/(TTFont(base/'assets/Sexsmith.otf')['OS/2'].sCapHeight*90/1000)
for style in ['Medium','Bold']:
    font=TTFont(base/'assets/Sexsmith.otf')
    cmap=font.getBestCmap(); assert set(range(0x20,0x7f))<=set(cmap) and set(range(0x410,0x450))<=set(cmap)
    rawfont=(base/'assets/Sexsmith.otf').read_bytes()
    upem=font['head'].unitsPerEm; factor=90/upem
    hhea=font['hhea']; os2=font['OS/2']; post=font['post']
    metrics={'asc':hhea.ascent*factor,'desc':hhea.descent*factor,'cap':os2.sCapHeight*factor,'mean':os2.sxHeight*factor,'underline':post.underlinePosition*factor,'thickness':post.underlineThickness*factor}
    if style=='Medium':
        font_obj=asset.objects[1408]; tex_obj=asset.objects[429]; mat_obj=asset.objects[2]; tmp_obj=asset.objects[14291]
    else:
        font_obj=clone(1400); tex_obj=clone(432); mat_obj=clone(2); tmp_obj=clone(14290)
    font_data=font_obj.read_typetree(); font_data['m_Name']=f'Sexsmith {style}'; font_data['m_FontData']=rawfont; font_obj.save_typetree(font_data)
    tex_data=asset.objects[432].read_typetree(); tex_data['m_Name']=f'Sexsmith {style} Atlas'; tex_obj.save_typetree(tex_data)
    mat_data=mat_obj.read_typetree(); mat_data['m_Name']=f'Sexsmith {style} Material'
    for key,value in mat_data['m_SavedProperties']['m_TexEnvs']:
        if key=='_MainTex': value['m_Texture']=ptr(tex_obj)
    normal_weight=0.0
    mat_data['m_SavedProperties']['m_Floats']=[(key,1024.0 if key in ['_TextureWidth','_TextureHeight'] else normal_weight if key=='_WeightNormal' else 0.0 if key=='_WeightBold' else value) for key,value in mat_data['m_SavedProperties']['m_Floats']]
    mat_obj.save_typetree(mat_data)
    tmp_data=copy.deepcopy(original_trees[14291 if style=='Medium' else 14290])
    tmp_data['m_Name']=f'Sexsmith {style} SDF'
    tmp_data['m_Material']=ptr(mat_obj); tmp_data['m_SourceFontFile']=ptr(font_obj)
    tmp_data['m_SourceFontFileGUID']=uuid.uuid5(uuid.NAMESPACE_URL,'repo-font-v2-'+style).hex
    tmp_data['m_SourceFontFilePath']=''; tmp_data['m_AtlasPopulationMode']=1
    tmp_data['m_AtlasTextures']=[ptr(tex_obj)]; tmp_data['m_AtlasTextureIndex']=0
    tmp_data['m_IsMultiAtlasTexturesEnabled']=1
    tmp_data['m_GlyphTable']=[]; tmp_data['m_CharacterTable']=[]; tmp_data['m_UsedGlyphRects']=[]
    tmp_data['m_FreeGlyphRects']=[{'m_X':0,'m_Y':0,'m_Width':1023,'m_Height':1023}]
    for key in tmp_data['m_FontFeatureTable']: tmp_data['m_FontFeatureTable'][key]=[]
    if style=='Bold': tmp_data['m_FallbackFontAssetTable']=[]
    tmp_data['normalStyle']=normal_weight; tmp_data['boldStyle']=0.0
    # TMP adds this sampled-font-unit offset to every advance. At 90pt this is about 1% em.
    tmp_data['normalSpacingOffset']=0.9; tmp_data['boldSpacing']=0.0
    info=tmp_data['m_FaceInfo']; scale=56/metrics['cap']
    info.update(m_FamilyName='Sexsmith',m_StyleName=style,m_PointSize=90.0,m_UnitsPerEM=upem,m_Scale=scale,m_AscentLine=metrics['asc'],m_DescentLine=metrics['desc'],m_LineHeight=128.97000122070312/scale,m_CapLine=metrics['cap'],m_MeanLine=metrics['mean'],m_Baseline=0.0,m_SuperscriptOffset=metrics['asc'],m_SubscriptOffset=metrics['desc'],m_UnderlineOffset=metrics['underline'],m_UnderlineThickness=metrics['thickness'],m_StrikethroughOffset=metrics['mean']*0.4,m_StrikethroughThickness=metrics['thickness'])
    tmp_data['m_CreationSettings'].update(sourceFontFileGUID=tmp_data['m_SourceFontFileGUID'],characterSequence='',includeFontFeatures=1)
    variants.append({'style':style,'font':font_obj,'texture':tex_obj,'material':mat_obj,'asset':tmp_obj,'data':tmp_data,'character_count':len(cmap)})
for variant in variants:
    for weight in range(len(variant['data']['m_FontWeightTable'])):
        variant['data']['m_FontWeightTable'][weight]['regularTypeface']=ptr(variants[1]['asset']) if weight>=6 and variant['style']=='Medium' else {'m_FileID':0,'m_PathID':0}
        variant['data']['m_FontWeightTable'][weight]['italicTypeface']={'m_FileID':0,'m_PathID':0}
    variant['asset'].save_typetree(variant['data'],node)
d=copy.deepcopy(original_trees[14293]); d['m_FallbackFontAssetTable'].insert(0,ptr(variants[0]['asset'])); asset.objects[14293].save_typetree(d,node)
material_presets=[]
for ident,obj in asset.objects.items():
    if obj.type.name!='Material' or ident in [v['material'].path_id for v in variants]: continue
    data=obj.read_typetree()
    uses_primary=any(k=='_MainTex' and v['m_Texture']=={'m_FileID':0,'m_PathID':429} for k,v in data.get('m_SavedProperties',{}).get('m_TexEnvs',[]))
    if not uses_primary:continue
    data['m_SavedProperties']['m_Floats']=[(k,0.0 if k in ['_WeightNormal','_WeightBold'] else val) for k,val in data['m_SavedProperties']['m_Floats']]
    obj.save_typetree(data); material_presets.append(ident)
target.write_bytes(asset.save()); check=UnityPy.load(str(target)).file
changed=[i for i,raw in originals.items() if check.objects[i].get_raw_data()!=raw]
expected={1408,429,2,14291,14293}|set(material_presets)
assert set(changed)==expected,(changed,expected)
assert len(check.objects)==len(originals)+4
for v in variants:
    f=check.objects[v['font'].path_id].read_typetree(); decoded=TTFont(io.BytesIO(bytes(x&255 for x in f['m_FontData'])))
    assert decoded.getBestCmap()==TTFont(base/'assets/Sexsmith.otf').getBestCmap()
    t=check.objects[v['asset'].path_id].read_typetree(node)
    assert t['normalSpacingOffset']==check.objects[14291].read_typetree(node)['normalSpacingOffset']
manifest={'version':'3.0','build':'26294889','source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'patched_sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'normal_weight':0.0,'spacing_sample_units':0.9,'modified_original_objects':changed,'added_objects':sorted(set(check.objects)-set(originals)),'material_presets':material_presets,'font_characters':{v['style']:v['character_count'] for v in variants},'original_object_count':len(originals),'patched_object_count':len(check.objects)}
(target.parent/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(manifest,indent=2),flush=True)

