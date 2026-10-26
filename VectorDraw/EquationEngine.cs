using System.Globalization;
namespace VectorDraw;

// Parser matematico senza valutazione di codice esterno.
public sealed class EquationEngine
{
    private readonly string _source;
    private int _pos;
    private readonly Dictionary<string,double> _vars=new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<double> _evaluate;
    public EquationEngine(string expression)
    {
        _source=expression.Replace(" ","").Replace("π","pi");
        _evaluate=ParseExpression();
        if(_pos!=_source.Length)throw new FormatException("Simbolo inatteso alla posizione "+(_pos+1));
    }
    public double Evaluate(double x=0,double y=0,double z=0,double t=0)
    {
        _vars["x"]=x;_vars["y"]=y;_vars["z"]=z;_vars["t"]=t;
        var value=_evaluate();
        return double.IsFinite(value)?value:double.NaN;
    }
    private bool Take(char c)
    {
        if(_pos<_source.Length&&_source[_pos]==c){_pos++;return true;}
        return false;
    }
    private Func<double> ParseExpression()
    {
        var left=ParseTerm();
        while(true)
        {
            if(Take('+')){var l=left;var r=ParseTerm();left=()=>l()+r();}
            else if(Take('-')){var l=left;var r=ParseTerm();left=()=>l()-r();}
            else return left;
        }
    }
    private Func<double> ParseTerm()
    {
        var left=ParseSigned();
        while(true)
        {
            if(Take('*')){var l=left;var r=ParseSigned();left=()=>l()*r();}
            else if(Take('/')){var l=left;var r=ParseSigned();left=()=>l()/r();}
            else return left;
        }
    }
    private Func<double> ParseSigned()
    {
        if(Take('+'))return ParseSigned();
        if(Take('-')){var v=ParseSigned();return ()=>-v();}
        return ParsePower();
    }
    private Func<double> ParsePower()
    {
        var left=ParseAtom();
        if(Take('^')){var right=ParseSigned();return ()=>Math.Pow(left(),right());}
        return left;
    }
    private Func<double> ParseAtom()
    {
        if(Take('('))
        {
            var e=ParseExpression();
            if(!Take(')'))throw new FormatException("Parentesi di chiusura mancante.");
            return e;
        }
        int start=_pos;
        while(_pos<_source.Length&&(char.IsDigit(_source[_pos])||_source[_pos]=='.'||_source[_pos]==','))_pos++;
        if(_pos>start)
        {
            if(!double.TryParse(_source[start.._pos].Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out var number))
                throw new FormatException("Numero non valido.");
            return ()=>number;
        }
        start=_pos;
        while(_pos<_source.Length&&char.IsLetter(_source[_pos]))_pos++;
        if(start==_pos)throw new FormatException("Espressione non valida alla posizione "+(_pos+1));
        var name=_source[start.._pos].ToLowerInvariant();
        if(name=="pi")return ()=>Math.PI;
        if(name=="e")return ()=>Math.E;
        if(name is "x" or "y" or "z" or "t")return ()=>_vars[name];
        if(!Take('('))throw new FormatException("Funzione sconosciuta: "+name);
        var arg=ParseExpression();
        if(!Take(')'))throw new FormatException("Parentesi mancante dopo "+name);
        return name switch
        {
            "sin"=>()=>Math.Sin(arg()),"cos"=>()=>Math.Cos(arg()),
            "tan"=>()=>Math.Tan(arg()),"asin"=>()=>Math.Asin(arg()),
            "acos"=>()=>Math.Acos(arg()),"atan"=>()=>Math.Atan(arg()),
            "sqrt"=>()=>Math.Sqrt(arg()),"abs"=>()=>Math.Abs(arg()),
            "exp"=>()=>Math.Exp(arg()),"ln"=>()=>Math.Log(arg()),
            "log"=>()=>Math.Log10(arg()),"floor"=>()=>Math.Floor(arg()),
            "ceil"=>()=>Math.Ceiling(arg()),
            _=>throw new FormatException("Funzione non supportata: "+name)
        };
    }
}
