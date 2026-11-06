import bms.model.*;
import bms.player.beatoraja.play.*;

/** Executes original algorithm comparisons; the late predicate is separately identified as an inline guard. */
public final class AlgorithmOracle {
    public static void main(String[] args) {
        System.out.println("algorithm,press_us,replace,late_head");
        Note first = new NormalNote(0), second = new LongNote(0);
        first.setMicroTime(1000000); second.setMicroTime(1050000);
        long[][] table = JudgeProperty.LR2.getJudge(JudgeProperty.NoteType.NOTE,75,new int[]{100,100,100});
        for (JudgeAlgorithm algorithm : JudgeAlgorithm.values())
            for (long press : new long[]{1150000,1150001})
                System.out.printf("%s,%d,%s,%s%n", algorithm, press,
                    algorithm.compare(first,second,press,table), second.getMicroTime()-press < table[2][0]);
    }
}
