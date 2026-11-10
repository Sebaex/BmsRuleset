import bms.model.*;
import bms.player.beatoraja.play.*;
import java.util.Locale;

/** Executes original model validation and gauge state transitions, in percentage units. */
public final class GaugeOracle {
    public static void main(String[] args) {
        Locale.setDefault(Locale.ROOT);
        for (Class<?> source : new Class<?>[]{GrooveGauge.class, GaugeProperty.class, BMSPlayerRule.class, BMSModel.class})
            if (!source.getProtectionDomain().getCodeSource().getLocation().equals(GaugeOracle.class.getProtectionDomain().getCodeSource().getLocation()))
                throw new IllegalStateException("Reference class not compiled: " + source.getName());
        System.out.println("id,type,notes,input_total,total,seed_percent,step,judge,health_percent,qualified");
        for (int type = 0; type < 9; type++) emit("all-"+type,type,500,200,80,new int[]{0,1,2,3,4,5,0});
        emit("guts-below",3,1000,300,31.99f,new int[]{3,4,0});
        emit("guts-at",3,1000,300,32f,new int[]{3,4,0});
        emit("hazard-positive-subthreshold",5,1000,300,10.3f,new int[]{5,0});
        emit("hard-exact-two",3,1000,300,2f,new int[]{0,5,0});
        emit("hard-below-two",3,1000,300,1.99f,new int[]{0});
        for (int n : new int[]{100,400,401,500,600,601,1000}) emit("default-"+n,2,n,0,20,new int[]{0});
        emit("short-low-total",3,21,112,100,new int[]{3,4,0});
        emit("clear-exact",2,1000,300,80,new int[]{5,0});
    }
    private static void emit(String id,int type,int count,double total,float seed,int[] judges) {
        BMSModel model=new BMSModel(); model.setMode(Mode.BEAT_7K); model.setTotal(total);
        model.setJudgerankType(BMSModel.JudgeRankType.BMS_RANK); model.setJudgerank(2);
        model.setTotalType(BMSModel.TotalType.BMS);
        TimeLine[] lines=new TimeLine[count];
        for (int i=0;i<count;i++) { lines[i]=new TimeLine(i,i*1000L,8); lines[i].setNote(1,new NormalNote(0)); }
        model.setAllTimeLine(lines); BMSPlayerRule.validate(model);
        if (model.getTotalNotes()!=count) throw new IllegalStateException("Model note count mismatch");
        GrooveGauge gauge=new GrooveGauge(model,type,GaugeProperty.LR2); gauge.setValue(seed);
        row(id,type,count,total,model.getTotal(),seed,0,-1,gauge);
        for (int i=0;i<judges.length;i++) { gauge.update(judges[i]); row(id,type,count,total,model.getTotal(),seed,i+1,judges[i],gauge); }
    }
    private static void row(String id,int type,int n,double input,double total,float seed,int step,int judge,GrooveGauge gauge) {
        System.out.printf("%s,%d,%d,%.17g,%.17g,%.9g,%d,%d,%.9g,%s%n",id,type,n,input,total,seed,step,judge,gauge.getValue(),gauge.isQualified());
    }
}
