import bms.model.*;
import bms.player.beatoraja.play.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

/** Same BMS text, original parser and original gauge; no hand-assigned mine damage. */
public final class MineParserOracle {
    public static void main(String[] args) {
        for(Class<?> type:new Class<?>[]{BMSDecoder.class,Section.class,MineNote.class,GrooveGauge.class,GaugeProperty.class})
            if(!type.getProtectionDomain().getCodeSource().getLocation().equals(MineParserOracle.class.getProtectionDomain().getCodeSource().getLocation()))
                throw new IllegalStateException("Not freshly compiled: "+type);
        System.out.println("raw,damage,seed,after,alive,mine_time_us,note_count");
        for(String raw:new String[]{"01","0A","10"}) {
            String chart="#PLAYER 1\n#BPM 120\n#RANK 3\n#TOTAL 300\n#00118:01\n#001D2:"+raw+"\n";
            BMSModel model=new BMSDecoder(0).decode(chart.getBytes(StandardCharsets.US_ASCII),false,null);
            if(model==null) throw new IllegalStateException("Decode failed");
            BMSPlayerRule.validate(model);
            MineNote mine=null;
            for(TimeLine line:model.getAllTimeLines())for(int lane=0;lane<model.getMode().key;lane++)
                if(line.getNote(lane) instanceof MineNote found)mine=found;
            if(mine==null)throw new IllegalStateException("Missing mine");
            GrooveGauge gauge=new GrooveGauge(model,GrooveGauge.EXCLASS,GaugeProperty.LR2);
            gauge.setValue(11.5f);gauge.addValue((float)-mine.getDamage());
            System.out.printf(Locale.ROOT,"%s,%.17g,%.17g,%.17g,%s,%d,%d%n",raw,mine.getDamage(),11.5,(double)gauge.getValue(),gauge.getValue()>0,mine.getMicroTime(),model.getTotalNotes());
        }
    }
}
