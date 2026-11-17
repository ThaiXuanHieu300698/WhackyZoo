using System;
static class Band { static void Main(string[] a){ int rate; float[][] st; var x=Pcm.LoadMono(a[0],out rate,out st); double lo=double.Parse(a[1]), hi=double.Parse(a[2]); int W=rate/4;
 double al=1-Math.Exp(-2*Math.PI*lo/rate), ah=1-Math.Exp(-2*Math.PI*hi/rate), yl=0,yh=0; var sb=new System.Text.StringBuilder();
 for(int s0=0;s0+W<=x.Length;s0+=W){ double e=0; for(int i=s0;i<s0+W;i++){ yl+=al*(x[i]-yl); yh+=ah*(x[i]-yh); double b=yh-yl; e+=b*b;} sb.Append($"{s0/(double)rate:F2}:{10*Math.Log10(e/W+1e-12):F0} "); }
 Console.WriteLine(sb); }}
