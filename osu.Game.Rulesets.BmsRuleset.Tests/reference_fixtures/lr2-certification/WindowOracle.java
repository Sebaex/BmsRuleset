import bms.player.beatoraja.play.JudgeProperty;

/** Executes the original standalone JudgeProperty; contains no judgement scaling implementation. */
public final class WindowOracle {
    public static void main(String[] args) {
        System.out.println("kind,parameter,table,row,slow_us,fast_us");
        for (int rank = 0; rank < 5; rank++)
            emit("rank", rank, JudgeProperty.LR2.windowrule.judgerank[rank]);
        for (int exrank : new int[]{60, 200})
            emit("exrank", exrank, exrank * JudgeProperty.LR2.windowrule.judgerank[2] / 100);
    }

    private static void emit(String kind, int parameter, int rank) {
        for (JudgeProperty.NoteType type : JudgeProperty.NoteType.values()) {
            long[][] rows = JudgeProperty.LR2.getJudge(type, rank, new int[]{100, 100, 100});
            for (int i = 0; i < rows.length; i++)
                System.out.printf("%s,%d,%s,%d,%d,%d%n", kind, parameter, type, i, rows[i][0], rows[i][1]);
        }
    }
}
