using System; using System.Linq;
static class Timeline {
  // mỗi 0,5 s: năng lượng giữa (M) và bên (S) theo dải; S/M dải cao (thì thầm lệch tai), độ "có cao độ" dải 300-1200 (tiếng hú)
  static void Main(string[] a){ int rate; float[][] st; Pcm.LoadMono(a[0],out rate,out st); var L=st[0]; var R=st[1]; int n=L.Length;
    double step=double.Parse(a[1]); int W=(int)(step*rate);
    double aH=1-Math.Exp(-2*Math.PI*2500/rate), aL=1-Math.Exp(-2*Math.PI*300/rate), aM=1-Math.Exp(-2*Math.PI*1200/rate);
    double lm=0, ls=0, l3=0, l12=0;
    Console.WriteLine("  t     M_dB  S_dB  hiS-M  hiM  mid(300-1200)  bal(L-R)");
    for(int s0=0;s0+W<=n;s0+=W){ double em=0,es=0,hm=0,hs=0,mid=0,el=0,er=0; double hl=0,hr=0;
      for(int i=s0;i<s0+W;i++){ double m=(L[i]+R[i])/2, s=(L[i]-R[i])/2; em+=m*m; es+=s*s; lm+=aH*(m-lm); ls+=aH*(s-ls); double hmv=m-lm, hsv=s-ls; hm+=hmv*hmv; hs+=hsv*hsv;
        l3+=aL*(m-l3); l12+=aM*(m-l12); double b=l12-l3; mid+=b*b; el+=L[i]*L[i]; er+=R[i]*R[i]; }
      Func<double,double> db=v=>10*Math.Log10(v/W+1e-12);
      Console.WriteLine($"{s0/(double)rate,6:F1} {db(em),5:F0} {db(es),5:F0} {db(hs)-db(hm),6:F1} {db(hm),5:F0} {db(mid),6:F0} {db(el)-db(er),7:F1}");
    }}}
