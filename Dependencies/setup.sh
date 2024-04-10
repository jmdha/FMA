echo == Installing packages ==
echo 
apt install cmake gcc-multilib g++-multilib flex bison python3 curl automake dotnet-sdk-8.0 libfl-dev r-base
echo 
echo == Done! ==
echo 

echo == Installing Fast-Downward ==
echo 
git clone https://github.com/aibasel/downward fast-downward
cd fast-downward
python3 build.py
cd ..
echo 
echo == Done! ==
echo 

echo == Installing Stackelberg Planner ==
echo 
git clone -n --depth=1 --filter=tree:0 https://github.com/jamadaha/stackelberg-planner-sls.git stackelberg-planner
cd stackelberg-planner
git sparse-checkout set --no-cone src
git checkout
cd src
sed -e s/-Werror//g -i preprocess/Makefile
sed -e s/-Werror//g -i search/Makefile
bash build_all -j
cd ..
cd ..
echo 
echo == Done! ==
echo 

echo == Installing CPPDL ==
echo 
git clone https://gitlab.com/danfis/cpddl.git
cd cppdl
cp Makefile.config.tpl Makefile.config
./scripts/build.sh
cd ..
echo 
echo == Done! ==
echo 

echo == Installing Benchmarks ==
echo 
git clone https://github.com/ipc2023-learning/benchmarks learning-benchmarks
echo 
echo == Done! ==
echo 

echo == Installing R Packages ==
echo 
Rscript -e "install.packages(\"dplyr\");"
Rscript -e "install.packages(\"ggplot2\");"
Rscript -e "install.packages(\"xtable\");"
echo 
echo == Done! ==
echo 