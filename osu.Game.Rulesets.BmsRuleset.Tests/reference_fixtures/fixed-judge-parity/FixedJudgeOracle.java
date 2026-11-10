import bms.model.*;
import bms.player.beatoraja.play.*;
import java.util.*;

/** Executes the verbatim selection slice and original collector; does not execute the whole player. */
public final class FixedJudgeOracle {
    public static void main(String[] args) throws Exception {
        for (Class<?> source : new Class<?>[]{JudgeManager.class, JudgeProperty.class, JudgeAlgorithm.class,
                BMSPlayerRule.class, Note.class, NormalNote.class, LongNote.class, MineNote.class})
            if (!source.getProtectionDomain().getCodeSource().getLocation().equals(FixedJudgeOracle.class.getProtectionDomain().getCodeSource().getLocation()))
                throw new IllegalStateException("Reference class did not come from freshly compiled pinned sources: " + source);
        System.out.println("id,mode,parameter,scratch,mod,algorithm,press_us,types,times_us,judged,primary,judge,additional");
        for (boolean scratch : new boolean[]{false,true})
            for (String mod : new String[]{"none","no-good","no-great"})
                for (JudgeAlgorithm algorithm : JudgeAlgorithm.values()) {
                    emit("f099-fixed", "rank", 3, scratch, mod, algorithm, 1030000, "NNNN", new long[]{900000,1000000,1060000,1150000}, "0000");
                    emit("tie-right", "rank", 3, scratch, mod, algorithm, 1030001, "NNNN", new long[]{900000,1000000,1060000,1150000}, "0000");
                    emit("stacked", "rank", 3, scratch, mod, algorithm, 1030000, "NNN", new long[]{1000000,1000000,1060000}, "000");
                    emit("good-neighbour", "rank", 3, scratch, mod, algorithm, 1150000, "NNN", new long[]{1000000,1080000,1100000}, "000");
                    emit("late-long", "rank", 3, scratch, mod, algorithm, 1170001, "NL", new long[]{1000000,1050000}, "00");
                    emit("long-extra", "rank", 3, scratch, mod, algorithm, 850000, "NL", new long[]{1000000,1020000}, "00");
                    emit("long-primary", "rank", 3, scratch, mod, algorithm, 850000, "LN", new long[]{1000000,1020000}, "00");
                    emit("positive-long-pg", "rank", 3, scratch, mod, algorithm, 1010000, "L", new long[]{1000000}, "0");
                    emit("positive-long-great", "rank", 3, scratch, mod, algorithm, 1030000, "L", new long[]{1000000}, "0");
                    emit("modified-future", "rank", 3, scratch, mod, algorithm, 1050000, "NN", new long[]{1000000,1060000}, "00");
                    emit("modified-at", "rank", 3, scratch, mod, algorithm, 1060000, "NN", new long[]{1000000,1060000}, "00");
                    emit("judged", "rank", 3, scratch, mod, algorithm, 1030000, "NNN", new long[]{900000,1000000,1150000}, "100");
                    emit("empty-edge", "rank", 3, scratch, mod, algorithm, 0, "N", new long[]{1000000}, "0");
                    emit("empty-inside", "rank", 3, scratch, mod, algorithm, 1, "N", new long[]{1000000}, "0");
                    emit("empty-judged", "rank", 3, scratch, mod, algorithm, 850000, "N", new long[]{1000000}, "1");
                    for (long press : new long[]{799999,800000,1200000,1200001})
                        emit("bad-edge-"+press, "rank", 3, scratch, mod, algorithm, press, "N", new long[]{1000000}, "0");
                }
        for (boolean scratch : new boolean[]{false,true})
            for (JudgeAlgorithm algorithm : JudgeAlgorithm.values()) {
                for (int rank=0;rank<=4;rank++)
                    emit("global-rank", "rank", rank, scratch, "none", algorithm, 1030000, "NNNN", new long[]{900000,1000000,1060000,1150000}, "0000");
                for (int exrank : new int[]{60,87,100,143,200})
                    emit("global-exrank", "exrank", exrank, scratch, "none", algorithm, 1030000, "NNNN", new long[]{900000,1000000,1060000,1150000}, "0000");
            }
    }

    private static void emit(String id, String mode, int parameter, boolean scratch, String mod,
            JudgeAlgorithm algorithm, long press, String types, long[] times, String judged) throws Exception {
        BMSModel model = new BMSModel();
        model.setMode(Mode.BEAT_7K); model.setTotal(300);
        model.setJudgerank(parameter);
        model.setJudgerankType(mode.equals("rank") ? BMSModel.JudgeRankType.BMS_RANK : BMSModel.JudgeRankType.BMS_DEFEXRANK);
        BMSPlayerRule.validate(model);
        Note[] notes = new Note[times.length];
        for (int i=0;i<times.length;i++) {
            notes[i] = types.charAt(i)=='L' ? new LongNote(0) : new NormalNote(0);
            notes[i].setMicroTime(times[i]);
            notes[i].setState(judged.charAt(i)=='1' ? 1 : 0);
        }
        int[] rates = mod.equals("no-good") ? new int[]{100,100,0}
            : mod.equals("no-great") ? new int[]{100,0,0} : new int[]{100,100,100};
        // Original filter warnings for a judged primary go to stderr, preserving valid CSV output.
        java.io.PrintStream output = System.out;
        JudgeSelectionSlice.Result result;
        try {
            System.setOut(System.err);
            result = JudgeSelectionSlice.select(notes, press, model.getJudgerank(), scratch, algorithm, rates);
        } finally { System.setOut(output); }
        StringJoiner timestamps = new StringJoiner("|");
        for (long time : times) timestamps.add(Long.toString(time));
        System.out.printf(Locale.ROOT,"%s,%s,%d,%s,%s,%s,%d,%s,%s,%s,%d,%d,%s%n",
            id,mode,parameter,scratch,mod,algorithm,press,types,timestamps,judged,result.primary(),result.judge(),result.additional());
    }
}
