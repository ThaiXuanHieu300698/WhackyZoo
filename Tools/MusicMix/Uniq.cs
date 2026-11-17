using System; using System.Linq;
static class Uniq {
  // mỗi nửa ô nhịp: khoảng cách đặc trưng 8 dải tới nửa ô giống nhất ở vị trí khác (cùng pha ô nhịp) → đoạn "độc nhất" (giọng nói, tiếng hú…)
  static void Main(string[] a){ int rate; float[][] st; var x=Pcm.LoadMono(a[0],out rate,out st); double beat=double.Parse(a[1]), off=double.Parse(a[2]);
    int hop=512; double fps=rate/(double)hop;
    double[] cut={150,300,600,1200,2400,4800}; int F=x.Length/hop; var f=new double[F][]; var lp=new double[cut.Length]; var al=cut.Select(c=>1-Math.Exp(-2*Math.PI*c/rate)).ToArray();
    for(int k=0;k<F;k++){var e=new double[7]; for(int i=k*hop;i<(k+1)*hop;i++){double v=x[i],prev=0; for(int b=0;b<cut.Length;b++){lp[b]+=al[b]*(v-lp[b]); double bd=lp[b]-prev; e[b]+=bd*bd; prev=lp[b];} e[6]+=(v-prev)*(v-prev);} for(int b=0;b<7;b++) e[b]=Math.Log(1e-9+e[b]); f[k]=e;}
    double seg=beat*2; int W=(int)(seg*fps); int n=(int)((x.Length/(double)rate-off)/seg);
    for(int i=0;i<n;i++){ int fi=(int)((off+i*seg)*fps); if(fi+W>=F) break; double best=1e9; int bj=-1;
      for(int j=0;j<n;j++){ if(Math.Abs(j-i)<1) continue; int fj=(int)((off+j*seg)*fps); if(fj+W>=F) continue; double d=0; for(int k=0;k<W;k++) for(int b=0;b<7;b++){double t=f[fi+k][b]-f[fj+k][b]; d+=t*t;} d/=W*7; if(d<best){best=d;bj=j;} }
      Console.WriteLine($"{off+i*seg,6:F2}s  {best,6:F2} {new string('#',(int)Math.Min(60,best*8))}"); }
  }}
