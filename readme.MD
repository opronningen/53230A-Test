#53230A-Test
Various small specialized utilities to run particular experiments on the 53230A frequency counter. Of little interest to the general user, unless you want to replicate some of my experiments. This is very much a work in progress..

Prototype-in-progress quality, may not work as advertised. Or at all.

Needs a reference to 53230A.dll from "53230A Toolkit"

## Read3
1. Set the counter to frequency measurements, mode RCON or CONT, number of samples per trigger to n (\":samp:coun\"). :trig:sour IMM. 
2. Read3 sends a \"read?\", which triggers a new acquisition, and writesthe readings to STDOUT, each "block" on a separate line, separated by semi-colon. Number of columns equals :samp:count

I find my counter has a clear tendency to report a slightly different (usually higher) frequency on the first reading following a trigger, and that this difference is highly temperature dependant. Odd.

## MA \<n\>
n - number of points to average over.

Straight up moving average filter. Does not output any samples untill the averaging window is filled. Usage:

R | MA 100 > "filtered output.txt"

