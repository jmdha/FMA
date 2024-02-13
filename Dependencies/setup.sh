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

echo == Installing Benchmarks ==
echo 
git clone https://github.com/aibasel/downward-benchmarks downward-benchmarks
echo 
echo == Done! ==
echo 