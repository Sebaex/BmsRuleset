import bms.model.*;
import java.nio.charset.StandardCharsets;

/** Executes the fixed parser, including its original section/note construction. */
public final class ParserOracle {
    public static void main(String[] args) {
        for (Class<?> source : new Class<?>[]{BMSDecoder.class, BMSModel.class, LongNote.class})
            if (!source.getProtectionDomain().getCodeSource().getLocation().equals(ParserOracle.class.getProtectionDomain().getCodeSource().getLocation()))
                throw new IllegalStateException("Parser class must be compiled from fixed source");
        System.out.println("header,player_mode,locked_mode,scoring_notes");
        for (int header=-1;header<=3;header++) for (int player=0;player<=2;player++) {
            String text=(header<0?"":"#LNMODE "+header+"\n")+"#BPM 120\n#LNTYPE 1\n#00151:0102\n";
            BMSModel model=new BMSDecoder(player).decode(text.getBytes(StandardCharsets.US_ASCII),false,null);
            if (model==null) throw new IllegalStateException("Decode failed");
            System.out.printf("%d,%d,%d,%d%n",header,player,model.getLnmode(),model.getTotalNotes());
        }
    }
}
