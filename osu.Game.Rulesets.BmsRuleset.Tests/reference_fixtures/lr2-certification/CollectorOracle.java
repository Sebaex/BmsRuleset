import bms.model.*;
import bms.player.beatoraja.play.*;
import java.lang.reflect.*;
import java.util.*;

/** Invokes the original private collector, without constructing a graphical player or copying its filter. */
public final class CollectorOracle {
    public static void main(String[] args) throws Exception {
        for (Class<?> source : new Class<?>[]{JudgeManager.class, JudgeProperty.class, Note.class, LongNote.class})
            if (!source.getProtectionDomain().getCodeSource().getLocation().equals(CollectorOracle.class.getProtectionDomain().getCodeSource().getLocation()))
                throw new IllegalStateException("Original reference classes must be compiled into the oracle output: " + source.getName());
        System.out.println("id,press_ms,primary,types,times_ms,additional_indices");
        emit("early", 850, 0, "NN", new long[]{1000, 1020});
        emit("late", 1150, 1, "NN", new long[]{1000, 1020});
        emit("better", 1150, 1, "NNN", new long[]{1000, 1080, 1100});
        emit("long-primary", 850, 0, "LN", new long[]{1000, 1020});
        emit("long-extra", 850, 0, "NL", new long[]{1000, 1020});
        emit("equal", 850, 0, "NN", new long[]{1000, 1000});
        emit("leading-long", 1150, 2, "LNN", new long[]{1000, 1010, 1100});
        emit("long-after-normal", 1150, 2, "NLN", new long[]{1000, 1010, 1100});
        emit("three-early", 850, 0, "NNN", new long[]{1000, 1010, 1020});
    }

    private static void emit(String id, long press, int primary, String types, long[] times) throws Exception {
        Class<?> unsafeClass = Class.forName("sun.misc.Unsafe");
        Field singleton = unsafeClass.getDeclaredField("theUnsafe");
        singleton.setAccessible(true);
        // The collector never reads its enclosing player's state. Avoid native/audio initialization.
        Object manager = unsafeClass.getMethod("allocateInstance", Class.class).invoke(singleton.get(null), JudgeManager.class);
        Class<?> type = Class.forName("bms.player.beatoraja.play.JudgeManager$MultiBadCollector");
        Constructor<?> constructor = type.getDeclaredConstructor(JudgeManager.class);
        constructor.setAccessible(true);
        Object collector = constructor.newInstance(manager);
        Method setJudge = type.getDeclaredMethod("setJudge", long[][].class);
        setJudge.setAccessible(true);
        setJudge.invoke(collector, (Object)JudgeProperty.LR2.getJudge(JudgeProperty.NoteType.NOTE, 75, new int[]{100, 100, 100}));
        Method add = type.getDeclaredMethod("add", Note.class, long.class);
        add.setAccessible(true);
        Note[] notes = new Note[times.length];
        for (int i = 0; i < times.length; i++) {
            notes[i] = types.charAt(i) == 'L' ? new LongNote(0) : new NormalNote(0);
            notes[i].setMicroTime(times[i] * 1000);
            add.invoke(collector, notes[i], (times[i] - press) * 1000);
        }
        Method filter = type.getDeclaredMethod("filter", Note.class);
        filter.setAccessible(true);
        filter.invoke(collector, notes[primary]);
        Field arrayStart = type.getDeclaredField("arrayStart"), size = type.getDeclaredField("size"), list = type.getDeclaredField("noteList");
        arrayStart.setAccessible(true); size.setAccessible(true); list.setAccessible(true);
        Note[] retained = (Note[])list.get(collector);
        List<String> indices = new ArrayList<>();
        for (int i = arrayStart.getInt(collector); i < size.getInt(collector); i++)
            indices.add(Integer.toString(Arrays.asList(notes).indexOf(retained[i])));
        StringJoiner timestamps = new StringJoiner("|");
        for (long time : times) timestamps.add(Long.toString(time));
        System.out.printf("%s,%d,%d,%s,%s,%s%n", id, press, primary, types, timestamps, indices.isEmpty() ? "-" : String.join("|", indices));
    }
}
