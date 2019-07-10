# Axiom Profiler

A tool for visualising, analysing and understanding quantifier instantiations made via E-matching in a run of an SMT solver (at present, only [Z3](https://github.com/Z3Prover/z3) has been modified to provide the necessary log files). The tool takes a log file (which can be generated by Z3 by passing additional command-line options; see below) and presents information visually, primarily using a graph representation of the quantifier instantiations made and their causal connections. This graph can be filtered and explored in a variety of ways, and detailed explanations of individual quantifier instantiations are assembled and displayed in the left-hand panel. A range of customisations are available for aiding the presentation and understanding of this information, including explanations of equalities used to justify a quantifier instantiation. The Explain Path feature in the graph also produces automatically explanations of matching loops occurring in the SMT run. More details of the tool's features can be found [in this draft paper](http://people.inf.ethz.ch/summersa/wiki/lib/exe/fetch.php?media=papers:axiomprofiler.pdf)

Our tool was originally based on a tool called the [VCC Axiom Profiler](http://vcc.codeplex.com/SourceControl/latest#vcc/Tools/Z3Visualizer/), and was since developed via [Frederik Rothenberger's MSc project: Integration and Analysis of Alternative SMT Solvers for Software Verification](http://www.pm.inf.ethz.ch/education/student-projects/completedprojects.html) and by substantial work by Nils Becker, both supervised by [Alexander J. Summers](http://people.inf.ethz.ch/summersa/) who can be contacted with questions about the current version of the tool. We welcome bug reports and pull requests.

## Using on Windows

1.  Clone repository:

        hg clone https://bitbucket.org/viperproject/axiom-profiler
        
2.  Build from Visual Studio (also possible on the command-line): open source/AxiomProfiler.sln solution, and run the release build. Requires C# 6.0 features, .Net >= 4.5 (and a version of Visual Studio which supports this, e.g. >= 2017).
        
3.  Run the tool (either via Visual Studio, or by executing bin/Release/AxiomProfiler.exe)

## Using on Ubuntu
(note that the GUI of the tool currently suffers from some glitches when running under mono)

1.  Clone repository:

        hg clone https://bitbucket.org/viperproject/axiom-profiler
        cd axiom-profiler

2.  Install mono.
3.  Download NuGet:

        wget https://nuget.org/nuget.exe

4.  Install C# 6.0 compiler:

        mono ./nuget.exe install Microsoft.Net.Compilers

5.  Compile project:

        xbuild /p:Configuration=Release source/AxiomProfiler.sln

6.  Run Axiom Profiler:

        mono bin/Release/AxiomProfiler.exe

## Obtaining logs from Z3

NOTE: The Axiom Profiler requires at least version 4.8.5 of z3. To build the latest version of z3 from source follow the instructions at https://github.com/Z3Prover/z3.

Run Z3 with two extra command-line options:

    z3 trace=true proof=true ./input.smt2
(this will produce a log file called ./z3.log)
If you want to specify the target filename, you can pass a third option:

    z3 trace=true proof=true trace-file-name=foo.log ./input.smt2

NOTE: if this takes too long, it is possible to run the Axiom Profiler with a prefix of a valid log file - you could potentially kill the z3 process and obtain the corresponding partial log. Some users (especially on Windows) have reported that killing z3 can cause a lot of the file contents to disappear; if you observe this problem, it's recommended to copy the log file before killing the process.

Similarly, if you have a log file which takes too long to load into the Axiom Profiler, hitting Cancel will cause the tool to work with the portion loaded so far.

## Obtaining Z3 logs from various verification tools that use Z3 (feel free to add more)

To obtain a Z3 log with Boogie, use e.g:

    boogie /vcsCores:1 /z3opt:trace=true /z3opt:PROOF=true ./file.bpl
Note that you may also want to pass the /vcsCores:1 option to disable concurrency (since otherwise many Z3 instances may write to the same file)

To obtain a Z3 log with the Viper symbolic execution verifier (Silicon), use e.g:

    silicon --numberOfParallelVerifiers 1 --z3Args "trace=true proof=true" ./file.sil

If it complains about an unrecognized argument, try this:

    silicon --numberOfParallelVerifiers 1 --z3Args '"trace=true proof=true"' ./file.sil

To obtain a Z3 log with the Viper verification condition generation verifier (Carbon), use e.g:

    carbon --print ./file.bpl ./file.sil
    boogie /vcsCores:1 /z3opt:trace=true /z3opt:proof=true ./file.bpl

In all cases, the Z3 log should be stored in `./z3.log` (this can also be altered by correspondingly passing z3 the trace-file-name option described above)