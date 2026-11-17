using System; using System.Linq; using System.Collections.Generic;
static class Odd {
  // Lưới 1/8 ô nhịp; mỗi ô lưới: năng lượng 4 dải (M thấp-giữa 300-1200, M cao 2,5k+, S cao, S giữa). So với trung vị cùng vị trí trong các ô cùng phần → chỗ bất thường.
  static void Main(string[] a){ int rate; float[][] st; Pcm.LoadMono(a[0],out rate,out st); var L=st[0]; var R=st[1];
    double bar=4*0.5916, off=0.28; int nb=(int)((L.Length/(double)rate-off)/bar); int G=8; int cw=(int)(bar/G*rate);
    double aH=1-Math.Exp(-2*Math.PI*2500/rate), a3=1-Math.Exp(-2*Math.PI*300/rate), a12=1-Math.Exp(-2*Math.PI*1200/rate);
    var v=new double[nb,G,4]; double hm=0,hs=0,m3=0,m12=0,s3=0,s12=0;
    for(int b=0;b<nb;b++) for(int g=0;g<G;g++){ int s0=(int)((off+b*bar)*rate)+g*cw; double[] e=new double[4];
      for(int i=s0;i<s0+cw;i++){ double m=(L[i]+R[i])/2, s=(L[i]-R[i])/2; hm+=aH*(m-hm); hs+=aH*(s-hs); m3+=a3*(m-m3); m12+=a12*(m-m12); s3+=a3*(s-s3); s12+=a12*(s-s12);
        e[0]+=Math.Pow(m12-m3,2); e[1]+=Math.Pow(m-hm,2); e[2]+=Math.Pow(s-hs,2); e[3]+=Math.Pow(s12-s3,2);} for(int k=0;k<4;k++) v[b,g,k]=10*Math.Log10(e[k]/cw+1e-12); }
    string[] nm={"giữa(M)","cao(M)","cao(S)","giữa(S)"};
    Console.WriteLine("ô  "+string.Join(" | ",nm)+"   (mỗi ô 8 ký tự: lệch so với trung vị các ô cùng phần, '.'<2dB '+'2-4 '#'>4)");
    for(int b=0;b<nb;b++){ var line=new System.Text.StringBuilder($"{b,2} {off+b*bar,5:F1}s ");
      int lo=b<8?0:8, hi=b<8?8:nb;
      for(int k=0;k<4;k++){ for(int g=0;g<G;g++){ var col=new List<double>(); for(int j=lo;j<hi;j++) col.Add(v[j,g,k]); col.Sort(); double med=col[col.Count/2]; double d=v[b,g,k]-med; line.Append(d>4?'#':d>2?'+':d< -4?'_':'.'); } line.Append(" | "); }
      Console.WriteLine(line); }
  }}
