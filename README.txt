PWB Fuel Balancer for KSP 1.0.5.1028
version 0.1.2

The PWB Fuel Balancer allows an optimum location for the Center of Mass of a vessel to be set in the VAB or SPH and then for fuel to be pumped between tanks in flight in order to move the CoM back to that location when they are part full. This is intended to allow for perfect RSC control, allowing for rotation without translation and translation without rotation, assuming that the RCS thrusters have been perfectly located with reference to the CoM location. It is recommended that you also use the RCS Build Aid mod to exactly location the RCS thrusters. It has also been found to be useful for helping spaceplanes maintain balance as they consume fuel.
It is possible to display a (green) marker to indicate the centre of mass in the VAB/SPH by mousing over the part and pressing D. Set the optimum CoM location buy turning of CoM in the VAB/SPH, mousong over the part and pressing M.
You can display the CoM marker and optimum CoM marker inflight by right clicking on the park, and choosing "toggle marker" 
There is also a GUI that can be activated via the app launcher that allows to the manual positioning of the CoM target, both in the VAB and in flight.

Release History:

0.1.2
* test version by Skalou

* reworked the PWBControlBox.mu model.
* reworked the spheres modeles for the 3D CoM markers and textures.
* converted the textures to DDS to save RAM in increase the loading time.
* updated  the .cfg to KSP 1.0.5 and tweaked the parameters for carrer mode.

0.1.1
* Added display/hide target marker to the UI
* Added 2 save slots to the UI

0.1
* Rebuilt against KSP 0.90
* Added UI
* Enabled the naming of individual balancers
* Enabled the manual moving of balancers in flight and in the editor.

0.0.6
* Updated for KSP 0.23.5
* changed to be operating from launch rather than when its stage is staged.

0.0.5
* Added the part to the "Large Control" tech tree and tested for 0.22

0.0.4
* Fixed bugs that occur when the whole vessel is rotated in the editor. Unfortunately this will break backwards compatability (craft created with 0.0.3 will not have the correct CoM location set without returning to the VAB)
* Added markers to show CoM and optimum CoM locations in VAB and in flight

0.0.3
* Added prompts to the VAB / SPH to let the user know when the target CoM has been set
* Fixed a bug that caused the CoM to be set in the wrong location in the SPH
* Changed maintaince mode to continue attempting to balance even when a perfect solution is not possible.
* Changed the name of the part to reflect the fact that it is not just useful for RCS

0.0.2
* Added option of Maintain Balance - useful to account for CoM errors that arise when monopropellant is used during RCS manouvers
* Added the ability for the part to report when it is not possible to achieve a perfect balance.

0.0.1 
* Initial Release




Bug reports, suggestions and comments are welcome at pbowden@codepoet.org.uk Source code will be available in a future release.