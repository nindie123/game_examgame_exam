using System.Collections;
using System.Collections.Generic;
using BagElimination.Rules.Board;
using BagElimination.Rules.Simulation;
using UnityEngine;
namespace BagElimination.Presentation
{
    public sealed partial class BoardView
    {
        private readonly Dictionary<string,Sprite> _propSprites=new Dictionary<string,Sprite>();
        private Sprite PropSprite(BoardCell cell)
        {
            string key=cell.Type+"_"+cell.Direction;
            if(_propSprites.TryGetValue(key,out var cached)) return cached;
            var tex=new Texture2D(64,64); tex.filterMode=FilterMode.Bilinear;
            for(int y=0;y<64;y++) for(int x=0;x<64;x++)
            {
                float dx=x-31.5f,dy=y-31.5f; Color color=Color.clear;
                if(cell.Type==ElementType.Bomb)
                { if(dx*dx+dy*dy<700)color=dy>10?new Color(1,.3f,.3f):new Color(.3f,.15f,.5f); if(Mathf.Abs(dy)<6 && Mathf.Abs(dx)<27)color=Color.yellow; }
                else if(cell.Type==ElementType.Propeller)
                { if(Mathf.Abs(dx-dy)<9 || Mathf.Abs(dx+dy)<9)color=new Color(1,.75f,.1f); if(dx*dx+dy*dy<90)color=Color.red; }
                else
                {
                    float along=cell.Direction==RocketDirection.Horizontal?dx:dy, across=cell.Direction==RocketDirection.Horizontal?dy:dx;
                    if(Mathf.Abs(across)<11 && Mathf.Abs(along)<27)color=((int)(along+32)/9)%2==0?Color.red:Color.yellow;
                    if(along>20 && Mathf.Abs(across)<32-along)color=new Color(.25f,.55f,1);
                }
                tex.SetPixel(x,y,color);
            }
            tex.Apply(); var sprite=Sprite.Create(tex,new Rect(0,0,64,64),new Vector2(.5f,.5f),100); _propSprites[key]=sprite; return sprite;
        }
        public void ResetBoard()
        {
            StopAllCoroutines();
            foreach(Transform child in transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            _rendered=false; _board=null;
        }
        public IEnumerator PlayStep(SimulationStep step)
        {
            if(step.Kind=="Ready" || step.Kind=="Victory") yield break;
            var old=new Dictionary<int,Transform>(); var oldCells=new Dictionary<int,BoardCell>();
            foreach(var p in _board.EnumeratePositions())
            {
                var cell=_board.GetCell(p); var tr=_pieceTransforms[p.Row,p.Column];
                if(cell.IsOccupied && tr!=null) { old[cell.ObjectId]=tr; oldCells[cell.ObjectId]=cell; }
            }
            var next=new Transform[step.Board.Height,step.Board.Width];
            var moving=new List<Transform>(); var starts=new List<Vector3>(); var ends=new List<Vector3>();
            var retained=new HashSet<int>();
            foreach(var p in step.Board.EnumeratePositions())
            {
                var cell=step.Board.GetCell(p); if(!cell.IsOccupied)continue;
                retained.Add(cell.ObjectId); Vector3 target=CellCenter(p.Row,p.Column);
                if(!old.TryGetValue(cell.ObjectId,out var tr))
                {
                    var sr=CreateChildSprite("Piece_"+cell.ObjectId,PieceOrder,_boardRoot); sr.sprite=PieceSpriteFor(cell);
                    SetSpriteSize(sr,Vector2.one*CellWorldSize*PieceScaleFactor); tr=sr.transform;
                    tr.localPosition=step.Kind=="Refill"?target+Vector3.up*CellSpacing:target;
                }
                next[p.Row,p.Column]=tr; moving.Add(tr); starts.Add(tr.localPosition); ends.Add(target);
            }
            var removed=new List<Transform>(); var removedStart=new List<Vector3>(); var removedScale=new List<Vector3>(); var bags=new List<bool>();
            foreach(var entry in old) if(!retained.Contains(entry.Key))
            { removed.Add(entry.Value); removedStart.Add(entry.Value.position); removedScale.Add(entry.Value.localScale); bags.Add(oldCells[entry.Key].Type==ElementType.Backpack); }
            _board=step.Board; RefreshBackpackCount();
            if(step.Origin>=0) StartCoroutine(EffectPulse(step));
            float duration=step.Kind=="Fall"?.035f:step.Kind=="Refill"?.055f:step.Kind=="Swap"?.17f:.32f;
            float elapsed=0;
            while(elapsed<duration)
            {
                elapsed+=Time.deltaTime; float t=Mathf.Clamp01(elapsed/duration); float ease=t*t*(3-2*t);
                for(int i=0;i<moving.Count;i++) moving[i].localPosition=Vector3.Lerp(starts[i],ends[i],ease);
                for(int i=0;i<removed.Count;i++)
                {
                    if(bags[i]) removed[i].position=Vector3.Lerp(removedStart[i],_countText.transform.position,ease)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.5f;
                    removed[i].localScale=removedScale[i]*(1-t);
                }
                yield return null;
            }
            for(int i=0;i<moving.Count;i++)moving[i].localPosition=ends[i];
            foreach(var tr in removed)Destroy(tr.gameObject);
            _pieceTransforms=next;
        }
        private IEnumerator EffectPulse(SimulationStep step)
        {
            var flash=CreateChildSprite("Effect",40,_boardRoot); flash.sprite=WhiteSprite();
            flash.transform.localPosition=CellCenter(step.Origin/_board.Width,step.Origin%_board.Width);
            Vector3 size=step.Kind=="Rocket"?new Vector3(_board.Width*CellSpacing,.15f,1):new Vector3(2.5f,2.5f,1);
            // Read direction from the old displayed object at the trigger position where available.
            if(step.Kind=="Rocket")
            {
                var tr=_pieceTransforms[step.Origin/_board.Width,step.Origin%_board.Width];
                if(tr!=null && tr.GetComponent<SpriteRenderer>().sprite==PropSprite(BoardCell.CreateRocket(1,RocketDirection.Vertical))) size=new Vector3(.15f,_board.Height*CellSpacing,1);
            }
            for(float t=0;t<.25f;t+=Time.deltaTime)
            { flash.transform.localScale=size*(1+t); flash.color=new Color(1,.85f,.2f,.7f*(1-t/.25f)); yield return null; }
            Destroy(flash.gameObject);
        }
        public IEnumerator Celebrate()
        {
            yield return new WaitForSeconds(.35f); _boardRoot.SetActive(false);
            _hintText.transform.localPosition=new Vector3(0,1.8f,0); _hintText.characterSize=.13f; ShowHint("Great!");
            var reward=CreateChildSprite("Reward",70); reward.sprite=_goalSprite; SetSpriteSize(reward,new Vector2(2.5f,2.5f));
            for(float t=0;t<1;t+=Time.deltaTime) { reward.transform.localPosition=new Vector3(0,Mathf.Sin(t*Mathf.PI)*.5f,0); yield return null; }
        }
    }
}
